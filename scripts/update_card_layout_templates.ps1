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
    throw "update_card_layout_templates.ps1 currently supports the configured MySql database. Current DbProvider: $provider"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-card-template-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Set-Content -LiteralPath (Join-Path $tempDir "UpdateCardTemplates.csproj") -Encoding UTF8 -Value @'
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

const string IndexTemplate = """
<div class="app-container">
  <el-card shadow="never" class="mb-20" data-gen-id="gen_search_area">
    <el-form ref="queryFormRef" :model="queryParams" inline class="query-form">
      [gen.searchColumns]
      <el-form-item>
        <el-button type="primary" @click="handleSearch">{{ $t('search') }}</el-button>
        <el-button @click="handleReset">{{ $t('reset') }}</el-button>
      </el-form-item>
    </el-form>
  </el-card>

  <el-card shadow="never" data-gen-id="gen_list_area">
    <div class="toolbar" data-gen-id="gen_toolbar">
      <el-button type="primary" @click="handleAdd">{{ $t('add') }}</el-button>
    </div>

    <el-table :data="tableData" border stripe v-loading="loading">
      <el-table-column type="selection" width="50" />
      [gen.listColumns]
      <el-table-column :label="$t('operation')" width="220" fixed="right" data-gen-id="gen_operations">
        <template #default="scope">
          <el-button link type="primary" @click="handleDetail(scope.row)">{{ $t('detail') }}</el-button>
          <el-button link type="primary" @click="handleEdit(scope.row)">{{ $t('edit') }}</el-button>
          <el-button link type="danger" @click="handleDelete(scope.row)">{{ $t('delete') }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="pagination.page"
      v-model:page-size="pagination.pageSize"
      :total="pagination.total"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      @size-change="getList"
      @current-change="getList" />
  </el-card>
</div>
""";

const string AddTemplate = """
<div class="app-container">
  <el-card shadow="never" data-gen-id="gen_form_area">
    <template #header>
      <div class="card-header"><span>{{ $t('add') }} {{ $t('[gen.entityTitleKey]') }}</span></div>
    </template>
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
      <el-row :gutter="20">
        [gen.addColumns]
      </el-row>
    </el-form>
  </el-card>
[gen.relationCards]
    <div style="margin-top:20px;text-align:center" data-gen-id="gen_form_actions">
      <el-button type="primary" @click="handleSubmit">{{ $t('save') }}</el-button>
      <el-button @click="handleCancel">{{ $t('cancel') }}</el-button>
    </div>
[gen.relationDialogs]
</div>
""";

const string EditTemplate = """
<div class="app-container">
  <el-card shadow="never" data-gen-id="gen_form_area">
    <template #header>
      <div class="card-header"><span>{{ $t('edit') }} {{ $t('[gen.entityTitleKey]') }}</span></div>
    </template>
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
      <el-row :gutter="20">
        [gen.editColumns]
      </el-row>
    </el-form>
  </el-card>
[gen.relationCards]
    <div style="margin-top:20px;text-align:center" data-gen-id="gen_form_actions">
      <el-button type="primary" @click="handleSubmit">{{ $t('save') }}</el-button>
      <el-button @click="handleCancel">{{ $t('cancel') }}</el-button>
    </div>
[gen.relationDialogs]
</div>
""";

const string DetailTemplate = """
<div class="app-container">
  <el-card shadow="never" data-gen-id="gen_detail_area">
    <template #header>
      <div class="card-header"><span>{{ $t('[gen.entityTitleKey]') }} {{ $t('detail') }}</span></div>
    </template>
    <el-descriptions :column="2" border>
      [gen.detailColumns]
    </el-descriptions>
  </el-card>
[gen.relationCards]
    <div style="margin-top:20px;text-align:center" data-gen-id="gen_detail_actions">
      <el-button @click="handleBack">{{ $t('back') }}</el-button>
    </div>
</div>
""";

const string ChildCardTemplate = """
<el-card shadow="never" style="margin-top:20px" data-gen-id="gen_child_[relation.entityName]"><template #header><div class="card-header" style="display:flex;align-items:center;justify-content:space-between"><span>{{ $t('[relation.entityTitleKey]') }}</span><el-button type="primary" size="small" @click="handleAdd[relation.entityName]">{{ $t('add') }}</el-button></div></template><el-table :data="(form.[relation.collectionName] || []).filter(i => i.rowStatus !== 3)" border stripe>[relation.tableColumns]<el-table-column :label="$t('operation')" width="140"><template #default="scope"><el-button link type="primary" size="small" @click="handleEdit[relation.entityName](scope.row, scope.$index)">{{ $t('edit') }}</el-button><el-button link type="danger" size="small" @click="handleRemove[relation.entityName](scope.$index)">{{ $t('delete') }}</el-button></template></el-table-column></el-table></el-card>
""";

const string DetailChildCardTemplate = """
<el-card shadow="never" style="margin-top:20px" data-gen-id="gen_child_[relation.entityName]"><template #header><div class="card-header"><span>{{ $t('[relation.entityTitleKey]') }}</span></div></template><el-table :data="detail.[relation.collectionName] || []" border stripe>[relation.tableColumns]</el-table></el-card>
""";

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_page_templates_bak_{suffix} as select * from sys_page_templates");
        await ExecuteAsync($"create table sys_child_templates_bak_{suffix} as select * from sys_child_templates");
        Console.WriteLine($"backup: sys_page_templates_bak_{suffix}, sys_child_templates_bak_{suffix}");
    }

    await UpdatePageTemplateAsync("index", IndexTemplate);
    await UpdatePageTemplateAsync("add", AddTemplate);
    await UpdatePageTemplateAsync("edit", EditTemplate);
    await UpdatePageTemplateAsync("detail", DetailTemplate);

    await UpdateChildTemplateAsync("add", "card", ChildCardTemplate);
    await UpdateChildTemplateAsync("edit", "card", ChildCardTemplate);
    await UpdateChildTemplateAsync("detail", "card", DetailChildCardTemplate);

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

async Task UpdatePageTemplateAsync(string pageType, string html)
{
    await using var command = new MySqlCommand(
        """
        update sys_page_templates
        set html_content = @html, update_time = @now
        where page_type = @pageType and (is_deleted = 0 or is_deleted is null)
        """,
        connection,
        transaction);
    command.Parameters.AddWithValue("@html", html);
    command.Parameters.AddWithValue("@now", DateTime.Now);
    command.Parameters.AddWithValue("@pageType", pageType);
    var affected = await command.ExecuteNonQueryAsync();
    if (affected == 0)
    {
        throw new InvalidOperationException($"Page template not found: {pageType}");
    }
    Console.WriteLine($"updated page template: {pageType}");
}

async Task UpdateChildTemplateAsync(string pageType, string childType, string html)
{
    await using var command = new MySqlCommand(
        """
        update sys_child_templates
        set html_content = @html, update_time = @now
        where page_type = @pageType and child_type = @childType and (is_deleted = 0 or is_deleted is null)
        """,
        connection,
        transaction);
    command.Parameters.AddWithValue("@html", html);
    command.Parameters.AddWithValue("@now", DateTime.Now);
    command.Parameters.AddWithValue("@pageType", pageType);
    command.Parameters.AddWithValue("@childType", childType);
    var affected = await command.ExecuteNonQueryAsync();
    if (affected == 0)
    {
        throw new InvalidOperationException($"Child template not found: {pageType}/{childType}");
    }
    Console.WriteLine($"updated child template: {pageType}/{childType}");
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
