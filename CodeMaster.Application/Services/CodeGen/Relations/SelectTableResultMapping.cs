using System.Text.Json;

namespace CodeMaster.Application.Services.CodeGen.Relations;

public sealed class SelectTableResultMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}

public static class SelectTableResultMappingParser
{
    public static IReadOnlyList<SelectTableResultMapping> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<SelectTableResultMapping>();
        try
        {
            var mappings = JsonSerializer.Deserialize<List<SelectTableResultMapping>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (mappings == null)
                return Array.Empty<SelectTableResultMapping>();

            if (mappings.Any(item => string.IsNullOrWhiteSpace(item.SourceField) ||
                                     string.IsNullOrWhiteSpace(item.TargetField)))
            {
                throw new InvalidOperationException(
                    "Each select-table result mapping must provide sourceField and targetField.");
            }

            return mappings;
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException("Select-table result mappings must be a valid JSON array.", error);
        }
    }
}
