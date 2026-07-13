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
    throw "update_search_select_width_templates.ps1 currently supports the configured MySql database. Current DbProvider: $provider"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-search-select-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $migratorProject = Join-Path $repoRoot "CodeMaster.Migrator\CodeMaster.Migrator.csproj"

    Set-Content -LiteralPath (Join-Path $tempDir "UpdateSearchSelectWidthTemplates.csproj") -Encoding UTF8 -Value @"
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
using System.Collections;
using System.Reflection;
using System.Text.Json;
using CodeMaster.Migrator.SeedData.System;
using MySqlConnector;

var connectionString = args.Length > 0 ? args[0] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

var templates = LoadSearchSelectTemplates();

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_fct_bak_sel_width_{suffix} as select * from sys_field_control_templates");
        Console.WriteLine($"backup: sys_fct_bak_sel_width_{suffix}");
    }

    foreach (var template in templates)
    {
        await UpdateTemplateAsync(template.Type, template.Html, template.Sort);
    }

    await transaction.CommitAsync();
    Console.WriteLine("done");
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

static IReadOnlyList<(string Type, string Html, int Sort)> LoadSearchSelectTemplates()
{
    var templateType = typeof(TemplateModule);
    var emptyScript = JsonSerializer.Serialize(new
    {
        imports = Array.Empty<object>(),
        uses = Array.Empty<object>(),
        refs = Array.Empty<object>(),
        reactives = Array.Empty<object>(),
        functions = Array.Empty<object>(),
        hooks = Array.Empty<object>(),
        computed = Array.Empty<object>(),
        watches = Array.Empty<object>()
    });

    string GetTemplateScript(string methodName)
    {
        var method = templateType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(templateType.FullName, methodName);
        return (string)(method.Invoke(null, null) ?? throw new InvalidOperationException($"{methodName} returned null."));
    }

    var method = templateType.GetMethod("GetControlTemplates", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(templateType.FullName, "GetControlTemplates");

    var result = (IEnumerable)(method.Invoke(null, new object[]
    {
        emptyScript,
        GetTemplateScript("GetSelectDictScript"),
        GetTemplateScript("GetSelectTableScript"),
        GetTemplateScript("GetEditorScript"),
        GetTemplateScript("GetFileScript"),
        GetTemplateScript("GetImageScript")
    }) ?? throw new InvalidOperationException("GetControlTemplates returned null."));

    var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "select",
        "select-enum",
        "select-table"
    };

    var templates = new List<(string Type, string Html, int Sort)>();
    foreach (var item in result)
    {
        var itemType = item.GetType();
        var controlType = (string)(itemType.GetField("Item1")?.GetValue(item) ?? "");
        var section = (string)(itemType.GetField("Item2")?.GetValue(item) ?? "");
        if (!wanted.Contains(controlType) || !string.Equals(section, "search", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var html = (string)(itemType.GetField("Item3")?.GetValue(item) ?? "");
        var sort = (int)(itemType.GetField("Item5")?.GetValue(item) ?? 0);
        templates.Add((controlType, html, sort));
    }

    if (templates.Count != wanted.Count)
    {
        throw new InvalidOperationException($"Expected {wanted.Count} search select templates, found {templates.Count}.");
    }

    return templates;
}

async Task ExecuteAsync(string sql)
{
    await using var command = new MySqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}

async Task UpdateTemplateAsync(string controlType, string html, int sort)
{
    await using var command = new MySqlCommand(
        """
        update sys_field_control_templates
        set html_content = @html,
            sort = @sort,
            update_time = @now
        where control_type = @controlType
          and page_section = 'search'
          and (is_deleted = 0 or is_deleted is null)
        """,
        connection,
        transaction);

    command.Parameters.AddWithValue("@html", html);
    command.Parameters.AddWithValue("@sort", sort);
    command.Parameters.AddWithValue("@now", DateTime.Now);
    command.Parameters.AddWithValue("@controlType", controlType);

    var affected = await command.ExecuteNonQueryAsync();
    Console.WriteLine($"updated {controlType}/search: {affected}");
    if (affected == 0)
    {
        throw new InvalidOperationException($"Template not found: {controlType}/search");
    }
}
'@

    $argsForRun = @("run", "--project", (Join-Path $tempDir "UpdateSearchSelectWidthTemplates.csproj"), "--", $connectionString)
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
