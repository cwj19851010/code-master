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
    throw "update_editor_template_script.ps1 currently supports the configured MySql database. Current DbProvider: $provider"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-editor-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Set-Content -LiteralPath (Join-Path $tempDir "UpdateEditorTemplateScript.csproj") -Encoding UTF8 -Value @'
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
using System.Text.Json;
using MySqlConnector;

var connectionString = args.Length > 0 ? args[0] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

var editorScript = JsonSerializer.Serialize(new
{
    imports = new object[] {
        new { path = "vue", destructured = "ref, watch, onMounted, onUnmounted" },
        new { path = "@wangeditor/editor", destructured = "createEditor, createToolbar" },
        new { path = "@wangeditor/editor/dist/css/style.css" }
    },
    uses = Array.Empty<object>(),
    refs = new object[] {
        new { name = "[field.nameLower]EditorInstance", initialValue = "null" }
    },
    reactives = Array.Empty<object>(),
    functions = new object[] {
        new Dictionary<string, object> {
            ["name"] = "init[field.name]Editor",
            ["async"] = true,
            ["body"] = new[] {
                "const { createEditor, createToolbar } = await import('@wangeditor/editor');",
                "await import('@wangeditor/editor/dist/css/style.css');",
                "const editor = createEditor({",
                "  selector: '#[field.nameLower]Editor',",
                "  html: [field.formPrefix].[field.nameLower] || '',",
                "  config: {",
                "    placeholder: '[field.description]',",
                "    onChange(editor) { [field.formPrefix].[field.nameLower] = editor.getHtml(); }",
                "  }",
                "});",
                "createToolbar({ editor, selector: '#[field.nameLower]Toolbar', config: {} });",
                "[field.nameLower]EditorInstance.value = editor;"
            }
        }
    },
    hooks = new object[] {
        new { name = "onMounted", body = new[] { "init[field.name]Editor();" } },
        new { name = "onUnmounted", body = new[] { "if ([field.nameLower]EditorInstance.value) { [field.nameLower]EditorInstance.value.destroy(); }" } }
    },
    computed = Array.Empty<object>(),
    watches = new object[] {
        new {
            source = "[field.formPrefix].[field.nameLower]",
            body = new[] {
                "const editor = [field.nameLower]EditorInstance.value;",
                "const html = [field.formPrefix].[field.nameLower] || '';",
                "if (editor && editor.getHtml() !== html) { editor.setHtml(html); }"
            }
        }
    }
});

const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";

const string editorFormHtml = """
<el-col :xs="24" :sm="24"><el-form-item :label="$t('[field.labelKey]')" [field.prop] class="editor-form-item" data-gen-id="gen_field_[field.id]"><div class="editor-wrap" style="width:100%;border:1px solid var(--el-border-color);border-radius:4px;overflow:hidden"><div :id="'[field.nameLower]Toolbar'" class="editor-toolbar" style="border-bottom:1px solid var(--el-border-color)"></div><div :id="'[field.nameLower]Editor'" class="editor-content" style="min-height:220px"></div></div></el-form-item></el-col>
""";

const string editorListHtml = """
<el-table-column [field.prop] :label="$t('[field.labelKey]')" min-width="240" data-gen-id="gen_col_[field.id]"><template #default="scope"><div class="rich-text-cell" style="max-height:72px;overflow:hidden;line-height:1.5" v-html="scope.row.[field.nameLower] || '-'"></div></template></el-table-column>
""";

const string editorDetailHtml = """
<el-descriptions-item :label="$t('[field.labelKey]')" :span="2" data-gen-id="gen_field_[field.id]"><div class="rich-text-detail" style="max-width:100%;overflow:auto;line-height:1.6" v-html="detail.[field.nameLower] || '-'"></div></el-descriptions-item>
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

    var templates = new (string ControlType, string PageSection, string Html, string Script, int Sort)[]
    {
        ("editor", "add", editorFormHtml, editorScript, 23),
        ("editor", "edit", editorFormHtml, editorScript, 24),
        ("editor", "list", editorListHtml, emptyScript, 20),
        ("editor", "detail", editorDetailHtml, emptyScript, 21)
    };

    foreach (var template in templates)
    {
        await UpsertTemplateAsync(template.ControlType, template.PageSection, template.Html, template.Script, template.Sort);
        Console.WriteLine($"upserted {template.ControlType}/{template.PageSection}");
    }

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

async Task UpsertTemplateAsync(string controlType, string pageSection, string html, string script, int sort)
{
    await using (var update = new MySqlCommand(
        """
        update sys_field_control_templates
        set html_content = @html, script_sections = @script, sort = @sort, update_time = @now
        where control_type = @controlType and page_section = @pageSection and (is_deleted = 0 or is_deleted is null)
        """,
        connection,
        transaction))
    {
        update.Parameters.AddWithValue("@html", html.Trim());
        update.Parameters.AddWithValue("@script", script);
        update.Parameters.AddWithValue("@sort", sort);
        update.Parameters.AddWithValue("@now", DateTime.Now);
        update.Parameters.AddWithValue("@controlType", controlType);
        update.Parameters.AddWithValue("@pageSection", pageSection);
        var affected = await update.ExecuteNonQueryAsync();
        if (affected > 0) return;
    }

    var id = await NextIdAsync();
    await using var insert = new MySqlCommand(
        """
        insert into sys_field_control_templates
            (id, control_type, page_section, html_content, script_sections, sort, create_by, create_time, update_user_id, is_deleted)
        values
            (@id, @controlType, @pageSection, @html, @script, @sort, 'script', @now, 0, 0)
        """,
        connection,
        transaction);
    insert.Parameters.AddWithValue("@id", id);
    insert.Parameters.AddWithValue("@controlType", controlType);
    insert.Parameters.AddWithValue("@pageSection", pageSection);
    insert.Parameters.AddWithValue("@html", html.Trim());
    insert.Parameters.AddWithValue("@script", script);
    insert.Parameters.AddWithValue("@sort", sort);
    insert.Parameters.AddWithValue("@now", DateTime.Now);
    await insert.ExecuteNonQueryAsync();
}

async Task<long> NextIdAsync()
{
    await using var command = new MySqlCommand(
        "select coalesce(max(id), 2000000000000000000) + 1 from sys_field_control_templates",
        connection,
        transaction);
    var value = await command.ExecuteScalarAsync();
    return Convert.ToInt64(value);
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
