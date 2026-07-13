param(
    [string]$Config = "",
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Config)) {
    $Config = Join-Path $repoRoot "CodeMaster.Migrator\appsettings.json"
}

function Read-JsonWithLineComments {
    param([string]$Path)

    $raw = Get-Content -LiteralPath $Path -Raw
    $withoutComments = [regex]::Replace($raw, "(?m)^\s*//.*\r?\n?", "")
    return $withoutComments | ConvertFrom-Json
}

$configPath = [System.IO.Path]::GetFullPath($Config)
$appConfig = Read-JsonWithLineComments -Path $configPath
$provider = [string]$appConfig.DbProvider
$connectionString = [string]$appConfig.ConnectionStrings.DefaultConnection

if ($provider -notmatch "MySql") {
    throw "update_select_table_typed_display_templates.ps1 currently supports the configured MySql database. Current DbProvider: $provider"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-select-table-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Set-Content -LiteralPath (Join-Path $tempDir "UpdateSelectTableTemplates.csproj") -Encoding UTF8 -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MySqlConnector" Version="2.3.5" />
  </ItemGroup>
</Project>
'@

    Set-Content -LiteralPath (Join-Path $tempDir "Program.cs") -Encoding UTF8 -Value @'
using MySqlConnector;

var connectionString = args.Length > 0 ? args[0] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

const string ListTemplate = """
<el-table-column [v-for="displayField in field.displayFields"] :label="$t('[displayField.labelKey]')" data-gen-id="gen_col_[field.id]_[displayField.nameLower]"><template #default="scope">[displayField.listContent]</template></el-table-column>
""";

const string DetailTemplate = """
<el-descriptions-item [v-for="displayField in field.displayFields"] :label="$t('[displayField.labelKey]')" data-gen-id="gen_field_[field.id]_[displayField.nameLower]">[displayField.detailContent]</el-descriptions-item>
""";

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_field_control_templates_bak_{suffix} as select * from sys_field_control_templates");
        Console.WriteLine($"backup: sys_field_control_templates_bak_{suffix}");
    }

    await UpdateTemplateAsync("list", ListTemplate, 20);
    await UpdateTemplateAsync("detail", DetailTemplate, 35);

    await transaction.CommitAsync();
    Console.WriteLine("done");
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

async Task ExecuteAsync(string sql)
{
    await using var command = new MySqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}

async Task UpdateTemplateAsync(string pageSection, string html, int sort)
{
    await using var command = new MySqlCommand(
        """
        update sys_field_control_templates
        set html_content = @html, sort = @sort, update_time = @now
        where control_type = 'select-table' and page_section = @pageSection and (is_deleted = 0 or is_deleted is null)
        """,
        connection,
        transaction);
    command.Parameters.AddWithValue("@html", html);
    command.Parameters.AddWithValue("@sort", sort);
    command.Parameters.AddWithValue("@now", DateTime.Now);
    command.Parameters.AddWithValue("@pageSection", pageSection);
    var affected = await command.ExecuteNonQueryAsync();
    if (affected == 0)
    {
        throw new InvalidOperationException($"select-table template not found: {pageSection}");
    }
    Console.WriteLine($"updated select-table/{pageSection}");
}
'@

    dotnet restore $tempDir | Out-Host
    $argsForRun = @("run", "--project", $tempDir, "--no-restore", "--", $connectionString)
    if ($NoBackup) {
        $argsForRun += "--no-backup"
    }
    & dotnet @argsForRun
    if ($LASTEXITCODE -ne 0) {
        throw "Template update failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
