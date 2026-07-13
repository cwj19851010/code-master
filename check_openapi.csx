using System;
using System.Reflection;

var dll = Assembly.LoadFrom(@"C:\Users\cwj\.nuget\packages\microsoft.openapi\2.4.1\lib\net8.0\Microsoft.OpenApi.dll");
Console.WriteLine("Assembly: " + dll.FullName);
Console.WriteLine("\nNamespaces:");
foreach (var type in dll.GetTypes().Take(10))
{
    Console.WriteLine(type.Namespace + "." + type.Name);
}
