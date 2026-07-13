#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.Sqlite, 9.0.0"

using Microsoft.Data.Sqlite;

var dbPath = @"D:\MyHomeWorks\CodeMaster\CodeMaster.Migrator\CodeMaster_Test.db";
var connectionString = $"Data Source={dbPath}";

using var connection = new SqliteConnection(connectionString);
connection.Open();

// 检查列是否已存在
var checkCmd = connection.CreateCommand();
checkCmd.CommandText = "PRAGMA table_info(sys_module_entity)";
var hasColumn = false;

using (var reader = checkCmd.ExecuteReader())
{
    while (reader.Read())
    {
        if (reader.GetString(1) == "has_primary_key")
        {
            hasColumn = true;
            break;
        }
    }
}

if (!hasColumn)
{
    Console.WriteLine("Adding has_primary_key column...");
    var alterCmd = connection.CreateCommand();
    alterCmd.CommandText = "ALTER TABLE sys_module_entity ADD COLUMN has_primary_key INTEGER NOT NULL DEFAULT 1";
    alterCmd.ExecuteNonQuery();
    Console.WriteLine("Column added successfully!");
}
else
{
    Console.WriteLine("Column already exists.");
}
