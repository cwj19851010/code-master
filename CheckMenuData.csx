#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.Sqlite, 8.0.0"

using Microsoft.Data.Sqlite;

var connectionString = "Data Source=CodeMaster.WebApi/CodeMaster_Test.db";
using var connection = new SqliteConnection(connectionString);
connection.Open();

var command = connection.CreateCommand();
command.CommandText = @"
    SELECT Id, ParentId, MenuName, Path, MenuType, Visible
    FROM SysMenu
    WHERE MenuName LIKE '%用户%'
    ORDER BY ParentId, OrderNum
";

using var reader = command.ExecuteReader();
Console.WriteLine("Id\t\tParentId\tMenuName\t\tPath\t\tMenuType\tVisible");
Console.WriteLine("=".PadRight(100, '='));

while (reader.Read())
{
    var id = reader.GetInt64(0);
    var parentId = reader.IsDBNull(1) ? "NULL" : reader.GetInt64(1).ToString();
    var menuName = reader.GetString(2);
    var path = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
    var menuType = reader.GetString(4);
    var visible = reader.GetInt32(5);

    Console.WriteLine($"{id}\t{parentId}\t\t{menuName}\t\t{path}\t\t{menuType}\t\t{visible}");
}
