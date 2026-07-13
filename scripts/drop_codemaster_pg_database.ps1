[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Username = "postgres",
    [string]$Database = "CodeMasterDB",
    [string]$AdminDatabase = "postgres",
    [string]$Password,
    [string]$ConfirmDrop,
    [switch]$KeepTemp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($ConfirmDrop -ne $Database) {
    throw "Refusing to drop database '$Database'. Re-run with -ConfirmDrop $Database"
}

function Convert-SecureStringToPlainText {
    param([securestring]$SecureString)

    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $securePassword = Read-Host "PostgreSQL password for $Username@$HostName" -AsSecureString
    $Password = Convert-SecureStringToPlainText $securePassword
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "codemaster-drop-pgdb-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="9.0.0" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $tempRoot "DropPgDatabase.csproj") -Encoding UTF8

    @'
using Npgsql;

static string RequiredEnv(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing environment variable: {name}");
    }

    return value;
}

static string QuoteIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException("Database name cannot be empty.", nameof(value));
    }

    return "\"" + value.Replace("\"", "\"\"") + "\"";
}

var host = RequiredEnv("CODEMASTER_PG_HOST");
var port = int.Parse(RequiredEnv("CODEMASTER_PG_PORT"));
var username = RequiredEnv("CODEMASTER_PG_USERNAME");
var password = RequiredEnv("CODEMASTER_PG_PASSWORD");
var adminDatabase = RequiredEnv("CODEMASTER_PG_ADMIN_DATABASE");
var database = RequiredEnv("CODEMASTER_PG_DATABASE");

var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = host,
    Port = port,
    Username = username,
    Password = password,
    Database = adminDatabase,
    Pooling = false,
    Timeout = 15,
    CommandTimeout = 120
}.ConnectionString;

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

await using (var existsCommand = new NpgsqlCommand("select exists(select 1 from pg_database where datname = @database)", connection))
{
    existsCommand.Parameters.AddWithValue("database", database);
    var exists = (bool)(await existsCommand.ExecuteScalarAsync() ?? false);
    if (!exists)
    {
        Console.WriteLine($"Database '{database}' does not exist. Nothing to drop.");
        return;
    }
}

var quotedDatabase = QuoteIdentifier(database);

try
{
    await using var dropCommand = new NpgsqlCommand($"drop database {quotedDatabase} with (force)", connection);
    await dropCommand.ExecuteNonQueryAsync();
}
catch (PostgresException ex) when (ex.SqlState == "42601")
{
    await using (var terminateCommand = new NpgsqlCommand(@"
select pg_terminate_backend(pid)
from pg_stat_activity
where datname = @database
  and pid <> pg_backend_pid();", connection))
    {
        terminateCommand.Parameters.AddWithValue("database", database);
        await terminateCommand.ExecuteNonQueryAsync();
    }

    await using var dropCommand = new NpgsqlCommand($"drop database {quotedDatabase}", connection);
    await dropCommand.ExecuteNonQueryAsync();
}

Console.WriteLine($"Database '{database}' dropped successfully.");
'@ | Set-Content -LiteralPath (Join-Path $tempRoot "Program.cs") -Encoding UTF8

    $oldEnv = @{
        CODEMASTER_PG_HOST = $env:CODEMASTER_PG_HOST
        CODEMASTER_PG_PORT = $env:CODEMASTER_PG_PORT
        CODEMASTER_PG_USERNAME = $env:CODEMASTER_PG_USERNAME
        CODEMASTER_PG_PASSWORD = $env:CODEMASTER_PG_PASSWORD
        CODEMASTER_PG_ADMIN_DATABASE = $env:CODEMASTER_PG_ADMIN_DATABASE
        CODEMASTER_PG_DATABASE = $env:CODEMASTER_PG_DATABASE
    }

    $env:CODEMASTER_PG_HOST = $HostName
    $env:CODEMASTER_PG_PORT = [string]$Port
    $env:CODEMASTER_PG_USERNAME = $Username
    $env:CODEMASTER_PG_PASSWORD = $Password
    $env:CODEMASTER_PG_ADMIN_DATABASE = $AdminDatabase
    $env:CODEMASTER_PG_DATABASE = $Database

    try {
        if ($PSCmdlet.ShouldProcess("$HostName`:$Port/$Database", "DROP DATABASE")) {
            dotnet run --project $tempRoot --configuration Release
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet run failed with exit code $LASTEXITCODE"
            }
        }
    }
    finally {
        foreach ($key in $oldEnv.Keys) {
            [Environment]::SetEnvironmentVariable($key, $oldEnv[$key], "Process")
        }
    }
}
finally {
    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
