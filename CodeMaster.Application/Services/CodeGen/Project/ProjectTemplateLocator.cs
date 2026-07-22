using System.IO.Compression;
using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

internal static class ProjectTemplateLocator
{
    private const string TemplatePattern = "CodeMaster_Template_*.zip";
    private static readonly string[] RequiredFrameworkEntries =
    [
        "CodeMaster.Core/Entities/ITree.cs",
        "CodeMaster.Core/Services/IQueryApplicationService.cs",
        "CodeMaster.Core/Services/IReadOnlyTreeApplicationService.cs",
        "CodeMaster.Application/Services/QueryApplicationService.cs",
        "CodeMaster.Application/Services/ReadOnlyTreeApplicationService.cs"
    ];

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

            var solutionEntry = archive.Entries.First(entry =>
                NormalizeZipEntryName(entry.FullName).Equals("CodeMaster.sln", StringComparison.OrdinalIgnoreCase));
            var solutionContent = ReadEntryContent(solutionEntry);
            var missingSolutionProjects = Regex.Matches(
                    solutionContent,
                    "Project\\(\\\"[^\\\"]+\\\"\\)\\s*=\\s*\\\"[^\\\"]+\\\",\\s*\\\"(?<path>[^\\\"]+\\.csproj)\\\"",
                    RegexOptions.IgnoreCase)
                .Select(match => NormalizeZipEntryName(match.Groups["path"].Value))
                .Where(projectPath => !entries.Contains(projectPath, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingSolutionProjects.Count > 0)
            {
                throw new Exception(
                    $"Template solution references project files that are not included: {string.Join(", ", missingSolutionProjects)}");
            }

            var missingProjectReferences = archive.Entries
                .Where(entry => NormalizeZipEntryName(entry.FullName).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .SelectMany(entry =>
                {
                    var entryName = NormalizeZipEntryName(entry.FullName);
                    var projectDirectory = entryName.Contains('/')
                        ? entryName[..entryName.LastIndexOf('/')]
                        : string.Empty;
                    var projectContent = ReadEntryContent(entry);
                    return Regex.Matches(
                            projectContent,
                            "<ProjectReference\\s+Include=\\\"(?<path>[^\\\"]+)\\\"",
                            RegexOptions.IgnoreCase)
                        .Select(match => NormalizeZipRelativePath(projectDirectory, match.Groups["path"].Value))
                        .Where(projectPath => !entries.Contains(projectPath, StringComparer.OrdinalIgnoreCase));
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingProjectReferences.Count > 0)
            {
                throw new Exception(
                    $"Template project files reference projects that are not included: {string.Join(", ", missingProjectReferences)}");
            }

            if (!entries.Any(entry => entry.StartsWith("CodeMaster.WebApi/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.WebApi");

            if (!entries.Any(entry => entry.StartsWith("CodeMaster.Vue/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.Vue");

            var missingFrameworkEntries = RequiredFrameworkEntries
                .Where(required => !entries.Contains(required, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (missingFrameworkEntries.Count > 0)
            {
                throw new Exception(
                    $"Template ZIP is missing required framework files: {string.Join(", ", missingFrameworkEntries)}");
            }

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

            var frontendAgentEntry = archive.Entries.FirstOrDefault(entry =>
            {
                var entryName = NormalizeZipEntryName(entry.FullName);
                if (!entryName.Equals("CodeMaster.Vue/src/layout/index.vue", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var content = ReadEntryContent(entry);
                return content.Contains("AgentAssistant", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("<agent-drawer", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("agentVisible", StringComparison.Ordinal);
            });
            if (frontendAgentEntry != null)
            {
                throw new Exception(
                    "Template ZIP contains a CodeMaster Agent frontend reference without the excluded Agent component. " +
                    "Generate a new clean template before initializing a project.");
            }

            var migratorDbContextEntry = archive.Entries.FirstOrDefault(entry =>
                NormalizeZipEntryName(entry.FullName).Equals(
                    "CodeMaster.Migrator/Persistence/EfCore/CodeMasterDbContext.cs",
                    StringComparison.OrdinalIgnoreCase));
            if (migratorDbContextEntry != null)
            {
                var dbContextContent = ReadEntryContent(migratorDbContextEntry);
                if (dbContextContent.Contains("Domain.Entities.Ai", StringComparison.Ordinal) ||
                    dbContextContent.Contains("modelBuilder.Entity<Ai", StringComparison.Ordinal))
                {
                    throw new Exception(
                        "Template ZIP contains CodeMaster Agent entity references in the generated-project Migrator. " +
                        "Generate a new clean template before initializing a project.");
                }
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

    private static string NormalizeZipRelativePath(string baseDirectory, string relativePath)
    {
        var segments = $"{baseDirectory}/{relativePath.Replace('\\', '/')}"
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalized = new List<string>();

        foreach (var segment in segments)
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (normalized.Count > 0)
                {
                    normalized.RemoveAt(normalized.Count - 1);
                }

                continue;
            }

            normalized.Add(segment);
        }

        return string.Join('/', normalized);
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
