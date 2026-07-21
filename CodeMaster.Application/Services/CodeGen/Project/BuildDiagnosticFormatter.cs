using System.Text;
using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

public static partial class BuildDiagnosticFormatter
{
    private const int DefaultMaxLength = 12000;
    private const int TailLineCount = 80;

    public static string Summarize(string? output, string? failureMessage = null, int maxLength = DefaultMaxLength)
    {
        var text = output ?? string.Empty;
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var important = new List<string>();
        var metadataHints = BuildMetadataHints(lines);

        if (!string.IsNullOrWhiteSpace(failureMessage))
            important.Add(failureMessage.Trim());

        for (var index = 0; index < lines.Length; index++)
        {
            if (!ImportantLinePattern().IsMatch(lines[index]))
                continue;

            var start = Math.Max(0, index - 1);
            var end = Math.Min(lines.Length - 1, index + 2);
            for (var current = start; current <= end; current++)
                AddDistinct(important, lines[current]);
        }

        var tail = lines
            .Skip(Math.Max(0, lines.Length - TailLineCount))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var result = new StringBuilder();
        if (metadataHints.Count > 0)
        {
            result.AppendLine("=== CodeMaster metadata checks ===");
            foreach (var hint in metadataHints)
                result.AppendLine(hint);
        }

        if (important.Count > 0)
        {
            result.AppendLine("=== Build errors ===");
            foreach (var line in important)
                result.AppendLine(line);
        }

        if (tail.Count > 0)
        {
            result.AppendLine("=== Build log tail ===");
            foreach (var line in tail)
                result.AppendLine(line);
        }

        if (result.Length == 0)
            result.Append(string.IsNullOrWhiteSpace(failureMessage) ? text : failureMessage);

        return TrimPreservingEnd(result.ToString(), Math.Max(1000, maxLength));
    }

    private static List<string> BuildMetadataHints(IEnumerable<string> lines)
    {
        var hints = new List<string>();
        foreach (var line in lines)
        {
            var memberMatch = MissingMemberPattern().Match(line);
            if (memberMatch.Success)
            {
                AddDistinct(
                    hints,
                    $"Missing member detected: {memberMatch.Groups["type"].Value}.{memberMatch.Groups["member"].Value}. " +
                    "Check the entity field metadata and any handwritten references, then regenerate the affected entity or project.");
            }

            if (InterfaceMemberPattern().IsMatch(line))
            {
                AddDistinct(
                    hints,
                    "Generated interface members are incomplete. Check the entity capability switches and their system fields before regenerating.");
            }
        }

        return hints;
    }

    private static void AddDistinct(List<string> lines, string value)
    {
        var normalized = value.TrimEnd();
        if (string.IsNullOrWhiteSpace(normalized) || lines.Contains(normalized, StringComparer.Ordinal))
            return;

        lines.Add(normalized);
    }

    private static string TrimPreservingEnd(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        const string separator = "\n... diagnostic truncated ...\n";
        var headLength = Math.Max(1, maxLength * 2 / 3 - separator.Length);
        var tailLength = Math.Max(1, maxLength - headLength - separator.Length);
        return value[..headLength] + separator + value[^tailLength..];
    }

    [GeneratedRegex(
        @"(:\s*error\b|\berror\s+(?:CS|NU|NETSDK|MSB|TS|VITE)\d+|build failed|生成失败|error during build|failed to compile|exited with code|退出码)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImportantLinePattern();

    [GeneratedRegex(
        @"(?:'|“)(?<type>[^'”]+)(?:'|”)(?:\s+does not contain a definition for\s+|未包含)(?:'|“)(?<member>[^'”]+)(?:'|”)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MissingMemberPattern();

    [GeneratedRegex(
        @"(?:error\s+CS0535|does not implement interface member|未实现接口成员)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InterfaceMemberPattern();
}
