using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = "../CodeMaster.Migrator/CodeMaster_Test.db";
if (!File.Exists(dbPath))
{
    Console.WriteLine($"Database file not found: {dbPath}");
    return;
}

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// Check if sys_menu table exists
var checkTableCmd = connection.CreateCommand();
checkTableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='sys_menu';";
var tableName = checkTableCmd.ExecuteScalar() as string;

if (string.IsNullOrEmpty(tableName))
{
    Console.WriteLine("sys_menu table does not exist!");
    return;
}

Console.WriteLine("sys_menu table exists. Checking data...\n");

// Query menu data
var command = connection.CreateCommand();
command.CommandText = @"
    SELECT MenuId, ParentId, MenuName, MenuType, Path, Component, OrderNum
    FROM sys_menu
    ORDER BY OrderNum
    LIMIT 30;
";

using var reader = command.ExecuteReader();
Console.WriteLine("MenuId | ParentId | MenuName | MenuType | Path | Component | OrderNum");
Console.WriteLine(new string('-', 100));

int count = 0;
while (reader.Read())
{
    count++;
    Console.WriteLine($"{reader.GetInt64(0)} | {reader.GetInt64(1)} | {reader.GetString(2)} | {reader.GetString(3)} | {(reader.IsDBNull(4) ? "NULL" : reader.GetString(4))} | {(reader.IsDBNull(5) ? "NULL" : reader.GetString(5))} | {reader.GetInt32(6)}");
}

Console.WriteLine($"\nTotal records shown: {count}");

// Count total records
var countCmd = connection.CreateCommand();
countCmd.CommandText = "SELECT COUNT(*) FROM sys_menu;";
var totalCount = (long)countCmd.ExecuteScalar()!;
Console.WriteLine($"Total menu records in database: {totalCount}");
