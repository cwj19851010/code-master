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
    throw "update_page_lifecycle_templates.ps1 currently supports the configured MySql database. Current DbProvider: $provider"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-page-lifecycle-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $migratorProject = Join-Path $repoRoot "CodeMaster.Migrator\CodeMaster.Migrator.csproj"

    Set-Content -LiteralPath (Join-Path $tempDir "UpdatePageLifecycleTemplates.csproj") -Encoding UTF8 -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$migratorProject" />
    <PackageReference Include="MySqlConnector" Version="2.4.0" />
  </ItemGroup>
</Project>
"@

    Set-Content -LiteralPath (Join-Path $tempDir "Program.cs") -Encoding UTF8 -Value @'
using System.Reflection;
using CodeMaster.Migrator.SeedData.System;
using MySqlConnector;

var connectionString = args.Length > 0 ? args[0] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

var templateType = typeof(TemplateModule);
var indexScript = GetTemplateScript("GetIndexScript");
var addScript = GetTemplateScript("GetAddScript");

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_page_templates_bak_lifecycle_{suffix} as select * from sys_page_templates");
        Console.WriteLine($"backup: sys_page_templates_bak_lifecycle_{suffix}");
    }

    await UpdatePageScriptAsync("index", indexScript);
    await UpdatePageScriptAsync("add", addScript);

    await transaction.CommitAsync();
    Console.WriteLine("updated page lifecycle templates: index, add");
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

string GetTemplateScript(string methodName)
{
    var method = templateType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(templateType.FullName, methodName);
    return (string)(method.Invoke(null, null) ?? throw new InvalidOperationException($"{methodName} returned null."));
}

async Task UpdatePageScriptAsync(string pageType, string script)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        update sys_page_templates
        set script_sections = @script,
            update_time = @now
        where page_type = @pageType
          and is_deleted = 0
        """;
    command.Parameters.AddWithValue("@script", script);
    command.Parameters.AddWithValue("@now", DateTime.Now);
    command.Parameters.AddWithValue("@pageType", pageType);

    var affected = await command.ExecuteNonQueryAsync();
    Console.WriteLine($"{pageType}: {affected}");
    if (affected == 0)
    {
        throw new InvalidOperationException($"No sys_page_templates row updated for page_type={pageType}.");
    }
}

async Task ExecuteAsync(string sql)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}
'@

    $args = @("run", "--project", (Join-Path $tempDir "UpdatePageLifecycleTemplates.csproj"), "--", $connectionString)
    if ($NoBackup) {
        $args += "--no-backup"
    }

    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
