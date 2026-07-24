namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// Normalizes common model and database type aliases to valid C# type names
/// while preserving custom types such as project enums.
/// </summary>
public static class CSharpDataTypeNormalizer
{
    public static string Normalize(string? value, string fallback = "string")
    {
        var dataType = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var isNullable = dataType.EndsWith('?');
        var typeName = isNullable ? dataType[..^1].Trim() : dataType;

        if (typeName.StartsWith("global::System.", StringComparison.OrdinalIgnoreCase))
            typeName = typeName[15..];
        else if (typeName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            typeName = typeName[7..];

        var normalized = typeName.ToLowerInvariant() switch
        {
            "string" or "text" or "varchar" or "nvarchar" => "string",
            "bool" or "boolean" => "bool",
            "byte" => "byte",
            "sbyte" => "sbyte",
            "short" or "int16" or "smallint" => "short",
            "ushort" or "uint16" => "ushort",
            "int" or "int32" or "integer" => "int",
            "uint" or "uint32" => "uint",
            "long" or "int64" or "bigint" => "long",
            "ulong" or "uint64" => "ulong",
            "float" or "single" or "real" => "float",
            "double" => "double",
            "decimal" or "numeric" or "money" => "decimal",
            "char" => "char",
            "datetime" or "date" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "dateonly" => "DateOnly",
            "timeonly" or "time" => "TimeOnly",
            "timespan" => "TimeSpan",
            "guid" or "uuid" => "Guid",
            "object" => "object",
            _ => typeName
        };

        return isNullable ? $"{normalized}?" : normalized;
    }
}
