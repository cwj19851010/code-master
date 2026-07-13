param(
    [string]$Config = "",
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Config)) {
    $Config = Join-Path $repoRoot "CodeMaster.WebApi\appsettings.json"
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

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("codemaster-project-permission-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    Set-Content -LiteralPath (Join-Path $tempDir "UpdateProjectPermissions.csproj") -Encoding UTF8 -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MySqlConnector" Version="2.4.0" />
    <PackageReference Include="Npgsql" Version="9.0.0" />
  </ItemGroup>
</Project>
"@

    Set-Content -LiteralPath (Join-Path $tempDir "Program.cs") -Encoding UTF8 -Value @'
using System.Data.Common;
using MySqlConnector;
using Npgsql;

var provider = args.Length > 0 ? args[0] : throw new ArgumentException("Missing provider.");
var connectionString = args.Length > 1 ? args[1] : throw new ArgumentException("Missing connection string.");
var noBackup = args.Any(x => string.Equals(x, "--no-backup", StringComparison.OrdinalIgnoreCase));

var actions = new[]
{
    new ProjectAction("Delete Project", "codegen:project:delete", 10),
    new ProjectAction("Export Template", "codegen:project:export", 11),
    new ProjectAction("Initialize Project", "codegen:project:initialize", 12),
    new ProjectAction("Start Project", "codegen:project:start", 13),
    new ProjectAction("Stop Project", "codegen:project:stop", 14),
    new ProjectAction("Migrate Database", "codegen:project:migrate", 15),
    new ProjectAction("Build Project", "codegen:project:build", 16)
};

await using var connection = CreateConnection(provider, connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    if (!noBackup)
    {
        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await ExecuteAsync($"create table sys_menus_bak_project_permissions_{suffix} as select * from sys_menus");
        await ExecuteAsync($"create table sys_role_menus_bak_project_permissions_{suffix} as select * from sys_role_menus");
        Console.WriteLine($"backup: sys_menus_bak_project_permissions_{suffix}, sys_role_menus_bak_project_permissions_{suffix}");
    }

    var listMenuId = await ScalarAsync<long?>(@"
select id
from sys_menus
where path = 'project'
  and component = 'codegen/project/index'
  and menu_type = 'C'
order by id
limit 1");
    if (listMenuId is null)
    {
        throw new InvalidOperationException("Project list menu not found: path=project, component=codegen/project/index.");
    }

    var menuIds = new List<long> { listMenuId.Value };
    foreach (var action in actions)
    {
        var menuId = await ScalarAsync<long?>("select id from sys_menus where perms = @perms order by id limit 1", ("perms", action.Perms));
        if (menuId is null)
        {
            menuId = NewId();
            await ExecuteAsync(@"
insert into sys_menus (
    id, menu_name, title_key, order_num, path, component, query, is_frame, is_cache,
    menu_type, visible, status, perms, icon, menu_scope, create_time, is_deleted,
    parent_id, ancestors
) values (
    @id, @menuName, null, @orderNum, null, null, null, 0, false,
    'F', false, 0, @perms, null, 2, @createTime, false,
    @parentId, null
)",
                ("id", menuId.Value),
                ("menuName", action.Name),
                ("orderNum", action.OrderNum),
                ("perms", action.Perms),
                ("createTime", DateTime.UtcNow),
                ("parentId", listMenuId.Value));
            Console.WriteLine($"insert menu: {action.Perms}");
        }
        else
        {
            await ExecuteAsync(@"
update sys_menus
set parent_id = @parentId,
    menu_type = 'F',
    visible = false,
    status = 0,
    menu_scope = 2,
    is_deleted = false
where id = @id",
                ("id", menuId.Value),
                ("parentId", listMenuId.Value));
            Console.WriteLine($"update menu: {action.Perms}");
        }

        menuIds.Add(menuId.Value);
    }

    var roleIds = await ListLongAsync("select id from sys_roles where role_key in ('admin', 'tenant_owner') and is_deleted = false");
    foreach (var roleId in roleIds)
    {
        foreach (var menuId in menuIds)
        {
            var exists = await ScalarAsync<long?>(
                "select 1 from sys_role_menus where role_id = @roleId and menu_id = @menuId limit 1",
                ("roleId", roleId),
                ("menuId", menuId));
            if (exists is null)
            {
                await ExecuteAsync(
                    "insert into sys_role_menus (role_id, menu_id) values (@roleId, @menuId)",
                    ("roleId", roleId),
                    ("menuId", menuId));
            }
        }
    }

    await transaction.CommitAsync();
    Console.WriteLine($"done: {menuIds.Count} project menus assigned to {roleIds.Count} roles.");
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

DbConnection CreateConnection(string provider, string connectionString)
{
    if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        return new MySqlConnection(connectionString);
    if (provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase) || provider.Contains("PgSql", StringComparison.OrdinalIgnoreCase))
        return new NpgsqlConnection(connectionString);

    throw new NotSupportedException($"Unsupported provider for this script: {provider}");
}

async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    AddParameters(command, parameters);
    await command.ExecuteNonQueryAsync();
}

async Task<T?> ScalarAsync<T>(string sql, params (string Name, object? Value)[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    AddParameters(command, parameters);
    var value = await command.ExecuteScalarAsync();
    if (value == null || value == DBNull.Value)
        return default;
    return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
}

async Task<List<long>> ListLongAsync(string sql, params (string Name, object? Value)[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    AddParameters(command, parameters);
    await using var reader = await command.ExecuteReaderAsync();
    var values = new List<long>();
    while (await reader.ReadAsync())
    {
        values.Add(reader.GetInt64(0));
    }
    return values;
}

static void AddParameters(DbCommand command, params (string Name, object? Value)[] parameters)
{
    foreach (var (name, value) in parameters)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

static long NewId()
{
    Span<byte> bytes = stackalloc byte[8];
    System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
    var value = BitConverter.ToInt64(bytes);
    value = value == long.MinValue ? long.MaxValue : Math.Abs(value);
    return value % 8_000_000_000_000_000_000L + 1_000_000_000_000_000L;
}

sealed record ProjectAction(string Name, string Perms, int OrderNum);
'@

    $arguments = @($provider, $connectionString)
    if ($NoBackup) {
        $arguments += "--no-backup"
    }

    dotnet run --project $tempDir --configuration Release -- @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
