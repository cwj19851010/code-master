using System.IO.Compression;

namespace CodeMaster.Application.Services.CodeGen;

internal static class ProjectTemplateLocator
{
    private const string TemplatePattern = "CodeMaster_Template_*.zip";

    public static string ResolveWritableDirectory(
        string? configuredPath,
        string? currentDirectory = null,
        string? appBaseDirectory = null)
    {
        currentDirectory ??= Directory.GetCurrentDirectory();
        appBaseDirectory ??= AppContext.BaseDirectory;

        if (!string.IsNullOrWhiteSpace(configuredPath))
            return ResolveConfiguredPath(configuredPath, currentDirectory);

        var solutionRoot = FindSolutionRoot(currentDirectory) ?? FindSolutionRoot(appBaseDirectory);
        return solutionRoot != null
            ? Path.Combine(solutionRoot, "Templates")
            : Path.Combine(appBaseDirectory, "Templates");
    }

    public static string FindLatestValidTemplateZip(
        string? configuredPath,
        string? currentDirectory = null,
        string? appBaseDirectory = null)
    {
        currentDirectory ??= Directory.GetCurrentDirectory();
        appBaseDirectory ??= AppContext.BaseDirectory;

        var directories = GetCandidateDirectories(configuredPath, currentDirectory, appBaseDirectory);
        var templateFiles = directories
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, TemplatePattern))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        if (templateFiles.Count == 0)
            throw new Exception($"No project template ZIP found. Searched: {string.Join(", ", directories)}");

        var invalidTemplates = new List<string>();
        foreach (var templateFile in templateFiles)
        {
            try
            {
                ValidateTemplateZip(templateFile);
                return templateFile;
            }
            catch (Exception ex)
            {
                invalidTemplates.Add($"{templateFile}: {ex.Message}");
            }
        }

        throw new Exception(
            "No valid project template ZIP found. Templates must not contain historical EF migrations or excluded management-only references. " +
            string.Join(" | ", invalidTemplates));
    }

    public static void ValidateTemplateZip(string templatePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(templatePath);
            var entries = archive.Entries
                .Select(entry => NormalizeZipEntryName(entry.FullName))
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .ToList();

            if (!entries.Contains("CodeMaster.sln", StringComparer.OrdinalIgnoreCase))
                throw new Exception("Template ZIP must contain CodeMaster.sln at the root");

            if (!entries.Any(entry => entry.StartsWith("CodeMaster.WebApi/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.WebApi");

            if (!entries.Any(entry => entry.StartsWith("CodeMaster.Vue/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.Vue");

            var migrationEntry = entries.FirstOrDefault(IsHistoricalMigrationEntry);
            if (migrationEntry != null)
            {
                throw new Exception(
                    $"Template ZIP contains historical EF migration file '{migrationEntry}'. " +
                    "Generated projects must create a fresh InitialCreate migration during initialization.");
            }

            var programEntry = archive.Entries.FirstOrDefault(entry =>
                NormalizeZipEntryName(entry.FullName).Equals(
                    "CodeMaster.WebApi/Program.cs",
                    StringComparison.OrdinalIgnoreCase));
            if (programEntry != null)
            {
                var programContent = ReadEntryContent(programEntry);
                if (programContent.Contains("McpToken", StringComparison.Ordinal))
                {
                    throw new Exception(
                        "Template ZIP contains MCP token registrations without their excluded implementation files. " +
                        "Generate a new clean template before initializing a project.");
                }
            }

            var frontendMcpEntry = archive.Entries.FirstOrDefault(entry =>
            {
                var entryName = NormalizeZipEntryName(entry.FullName);
                if (!entryName.Equals("CodeMaster.Vue/src/router/index.js", StringComparison.OrdinalIgnoreCase) &&
                    !entryName.Equals("CodeMaster.Vue/src/layout/index.vue", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var content = ReadEntryContent(entry);
                return content.Contains("mcp-token", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("mcpToken", StringComparison.OrdinalIgnoreCase);
            });
            if (frontendMcpEntry != null)
            {
                throw new Exception(
                    $"Template ZIP contains an MCP frontend reference in '{NormalizeZipEntryName(frontendMcpEntry.FullName)}' " +
                    "without the excluded MCP page. Generate a new clean template before initializing a project.");
            }
        }
        catch (InvalidDataException ex)
        {
            throw new Exception("Template file is not a valid ZIP archive", ex);
        }
    }

    private static IReadOnlyList<string> GetCandidateDirectories(
        string? configuredPath,
        string currentDirectory,
        string appBaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return new[] { ResolveConfiguredPath(configuredPath, currentDirectory) };

        var directories = new List<string>();
        var solutionRoot = FindSolutionRoot(currentDirectory) ?? FindSolutionRoot(appBaseDirectory);
        if (solutionRoot != null)
            directories.Add(Path.Combine(solutionRoot, "Templates"));

        directories.Add(Path.Combine(appBaseDirectory, "Templates"));
        directories.Add(Path.Combine(currentDirectory, "Templates"));

        return directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FindSolutionRoot(string startPath)
    {
        var directory = Directory.Exists(startPath)
            ? new DirectoryInfo(startPath)
            : new DirectoryInfo(Path.GetDirectoryName(startPath) ?? startPath);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CodeMaster.sln")) &&
                File.Exists(Path.Combine(directory.FullName, "template-config.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ResolveConfiguredPath(string configuredPath, string currentDirectory)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
        return Path.GetFullPath(expandedPath, currentDirectory);
    }

    private static string NormalizeZipEntryName(string entryName)
    {
        return entryName.Replace('\\', '/').TrimStart('/');
    }

    private static string ReadEntryContent(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static bool IsHistoricalMigrationEntry(string entryName)
    {
        return entryName.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) &&
               entryName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }
}
