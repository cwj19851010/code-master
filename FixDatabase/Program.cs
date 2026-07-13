using Microsoft.Data.Sqlite;

var dbPath = @"D:\MyHomeWorks\CodeMaster\CodeMaster.Migrator\CodeMaster_Test.db";
var connectionString = $"Data Source={dbPath}";

using var connection = new SqliteConnection(connectionString);
connection.Open();

// 需要添加的列
var columnsToAdd = new Dictionary<string, string>
{
    { "is_multiple", "INTEGER NOT NULL DEFAULT 0" },
    { "related_entity_name", "TEXT" },
    { "related_entity_id_field", "TEXT" },
    { "related_entity_display_fields", "TEXT" }
};

// 检查并添加缺失的列
foreach (var column in columnsToAdd)
{
    var checkCmd = connection.CreateCommand();
    checkCmd.CommandText = "PRAGMA table_info(sys_entity_field)";
    var hasColumn = false;

    using (var reader = checkCmd.ExecuteReader())
    {
        while (reader.Read())
        {
            if (reader.GetString(1) == column.Key)
            {
                hasColumn = true;
                break;
            }
        }
    }

    if (!hasColumn)
    {
        Console.WriteLine($"Adding {column.Key} column to sys_entity_field...");
        var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE sys_entity_field ADD COLUMN {column.Key} {column.Value}";
        alterCmd.ExecuteNonQuery();
        Console.WriteLine($"Column {column.Key} added successfully!");
    }
    else
    {
        Console.WriteLine($"Column {column.Key} already exists.");
    }
}

connection.Close();
Console.WriteLine("Done!");
