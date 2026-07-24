using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

public static partial class CSharpModuleNameValidator
{
    public const string Requirement =
        "ModuleName must be an ASCII PascalCase C# namespace segment, for example OrderManagement. Put Chinese display text in ModuleDescription.";

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && ModuleNamePattern().IsMatch(value.Trim());
    }

    public static string RequireValid(string? value)
    {
        var moduleName = value?.Trim();
        if (!IsValid(moduleName))
            throw new ArgumentException(Requirement, nameof(value));

        return moduleName!;
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ModuleNamePattern();
}
