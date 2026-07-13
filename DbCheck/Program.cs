using System;
using Microsoft.Data.Sqlite;

var dbPath = args.Length > 0 ? args[0] : "../CodeMaster.WebApi/CodeMaster.db";
var connectionString = $"Data Source={dbPath}";

Console.WriteLine($"Checking database: {dbPath}");
Console.WriteLine();

using var connection = new SqliteConnection(connectionString);
connection.Open();

// Check sys_menu table
Console.WriteLine("=== sys_menu table ===");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT menu_id, menu_name, title_key, parent_id, menu_type FROM sys_menu ORDER BY menu_id";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"ID: {reader.GetInt64(0)}, Name: {reader.GetString(1)}, TitleKey: {reader.GetString(2)}, ParentId: {reader.GetInt64(3)}, Type: {reader.GetString(4)}");
    }
}

Console.WriteLine();
Console.WriteLine("=== sys_lang_text table ===");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT lang_key, lang_code, text_value FROM sys_lang_text WHERE lang_key LIKE 'menu.%' ORDER BY lang_key, lang_code";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"Key: {reader.GetString(0)}, Lang: {reader.GetString(1)}, Value: {reader.GetString(2)}");
    }
}
