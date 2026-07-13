#r "nuget: Microsoft.Data.Sqlite, 8.0.0"
using Microsoft.Data.Sqlite;

var connectionString = "Data Source=CodeMaster.Migrator/CodeMaster_Test.db";
using var connection = new SqliteConnection(connectionString);
connection.Open();

var command = connection.CreateCommand();
command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

Console.WriteLine("Tables in database:");
using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"  - {reader.GetString(0)}");
}
