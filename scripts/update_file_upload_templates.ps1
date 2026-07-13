param(
    [string]$Config = "",
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Config)) {
    $Config = Join-Path $repoRoot "CodeMaster.WebApi\appsettings.Development.json"
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

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "Missing ConnectionStrings:DefaultConnection in $configPath"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-file-upload-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $migratorProject = Join-Path $repoRoot "CodeMaster.Migrator\CodeMaster.Migrator.csproj"

    Set-Content -LiteralPath (Join-Path $tempDir "UpdateFileUploadTemplates.csproj") -Encoding UTF8 -Value @"
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
    <PackageReference Include="Npgsql" Version="9.0.0" />
  </ItemGroup>
</Project>
"@

    Set-Content -LiteralPath (Join-Path $tempDir "Program.cs") -Encoding UTF8 -Value @'
using System.Collections;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using CodeMaster.Migrator.SeedData.System;
using MySqlConnector;
using Npgsql;

var provider = args.Length > 0 ? args[0] : throw new ArgumentException("Missing provider.");
var connectionString = args.Length > 1 ? args[1] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

var templates = LoadFileTemplates();

await using var connection = CreateConnection(provider, connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_fct_bak_file_upload_{suffix} as select * from sys_field_control_templates");
        Console.WriteLine($"backup: sys_fct_bak_file_upload_{suffix}");
    }

    foreach (var template in templates)
    {
        await UpsertTemplateAsync(template.ControlType, template.Section, template.Html, template.Script, template.Sort);
    }

    await transaction.CommitAsync();
    Console.WriteLine("done");
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

static DbConnection CreateConnection(string provider, string connectionString)
{
    if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        return new MySqlConnection(connectionString);
    if (provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase) || provider.Contains("PgSql", StringComparison.OrdinalIgnoreCase))
        return new NpgsqlConnection(connectionString);

    throw new NotSupportedException($"Unsupported provider for this script: {provider}");
}

static IReadOnlyList<(string ControlType, string Section, string Html, string Script, int Sort)> LoadFileTemplates()
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

    var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "file|add",
        "file|edit",
        "file|list",
        "file|detail",
        "image|list",
        "image|detail"
    };
    var templates = new List<(string ControlType, string Section, string Html, string Script, int Sort)>();

    foreach (var item in result)
    {
        var itemType = item.GetType();
        var controlType = (string)(itemType.GetField("Item1")?.GetValue(item) ?? "");
        var section = (string)(itemType.GetField("Item2")?.GetValue(item) ?? "");
        if (!targets.Contains($"{controlType}|{section}"))
        {
            continue;
        }

        templates.Add((
            controlType,
            section,
            (string)(itemType.GetField("Item3")?.GetValue(item) ?? ""),
            (string)(itemType.GetField("Item4")?.GetValue(item) ?? ""),
            (int)(itemType.GetField("Item5")?.GetValue(item) ?? 0)
        ));
    }

    if (templates.Count != targets.Count)
        throw new InvalidOperationException($"Expected {targets.Count} upload display templates, found {templates.Count}.");

    return templates;
}

async Task ExecuteAsync(string sql)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

DbParameter CreateParameter(DbCommand command, string name, object? value)
{
    var parameter = command.CreateParameter();
    parameter.ParameterName = name;
    parameter.Value = value ?? DBNull.Value;
    return parameter;
}

async Task UpsertTemplateAsync(string controlType, string section, string html, string script, int sort)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText =
        """
        update sys_field_control_templates
        set html_content = @html,
            script_sections = @script,
            sort = @sort,
            update_time = @now
        where control_type = @controlType
          and page_section = @pageSection
        """;

    command.Parameters.Add(CreateParameter(command, "@html", html));
    command.Parameters.Add(CreateParameter(command, "@script", script));
    command.Parameters.Add(CreateParameter(command, "@sort", sort));
    command.Parameters.Add(CreateParameter(command, "@now", DateTime.UtcNow));
    command.Parameters.Add(CreateParameter(command, "@controlType", controlType));
    command.Parameters.Add(CreateParameter(command, "@pageSection", section));

    var affected = await command.ExecuteNonQueryAsync();
    if (affected > 0)
    {
        Console.WriteLine($"updated {controlType}/{section}: {affected}");
        return;
    }

    await using var insert = connection.CreateCommand();
    insert.Transaction = transaction;
    insert.CommandText =
        """
        insert into sys_field_control_templates
            (id, control_type, page_section, html_content, script_sections, sort, is_deleted, create_time, update_time)
        values
            (@id, @controlType, @pageSection, @html, @script, @sort, @isDeleted, @now, @now)
        """;
    insert.Parameters.Add(CreateParameter(insert, "@id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000)));
    insert.Parameters.Add(CreateParameter(insert, "@controlType", controlType));
    insert.Parameters.Add(CreateParameter(insert, "@pageSection", section));
    insert.Parameters.Add(CreateParameter(insert, "@html", html));
    insert.Parameters.Add(CreateParameter(insert, "@script", script));
    insert.Parameters.Add(CreateParameter(insert, "@sort", sort));
    insert.Parameters.Add(CreateParameter(insert, "@isDeleted", false));
    insert.Parameters.Add(CreateParameter(insert, "@now", DateTime.UtcNow));
    await insert.ExecuteNonQueryAsync();
    Console.WriteLine($"inserted {controlType}/{section}: 1");
}
'@

    $argsForRun = @(
        "run",
        "--project",
        (Join-Path $tempDir "UpdateFileUploadTemplates.csproj"),
        "--",
        $provider,
        $connectionString
    )
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
