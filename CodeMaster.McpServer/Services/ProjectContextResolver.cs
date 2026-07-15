using System.Text.Json;

namespace CodeMaster.McpServer.Services;

public class ProjectContextResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ProjectContextInfo?> ResolveAsync(string? workspacePath = null)
    {
        var startPath = string.IsNullOrWhiteSpace(workspacePath)
            ? Directory.GetCurrentDirectory()
            : workspacePath;

        var directory = File.Exists(startPath)
            ? Path.GetDirectoryName(Path.GetFullPath(startPath))
            : Path.GetFullPath(startPath);

        while (!string.IsNullOrWhiteSpace(directory))
        {
            var contextPath = Path.Combine(directory, ".codemaster", "project-context.json");
            if (File.Exists(contextPath))
            {
                var json = await File.ReadAllTextAsync(contextPath);
                var context = JsonSerializer.Deserialize<ProjectContextInfo>(json, JsonOptions);
                if (context != null)
                {
                    context.ContextPath = contextPath;
                    context.ProjectRoot = directory;
                }

                return context;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }
}

public class ProjectContextInfo
{
    public int SchemaVersion { get; set; }

    public string? ServerBaseUrl { get; set; }

    public string? ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? DatabaseType { get; set; }

    public int FrontendPort { get; set; }

    public int BackendPort { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ContextPath { get; set; }

    public string? ProjectRoot { get; set; }
}
