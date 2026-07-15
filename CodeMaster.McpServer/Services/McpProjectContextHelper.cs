namespace CodeMaster.McpServer.Services;

public static class McpProjectContextHelper
{
    public static async Task<long> ResolveProjectIdAsync(
        ProjectContextResolver resolver,
        long projectId,
        string? workspacePath)
    {
        if (projectId > 0)
            return projectId;

        var context = await resolver.ResolveAsync(workspacePath);
        return long.TryParse(context?.ProjectId, out var resolvedProjectId)
            ? resolvedProjectId
            : 0;
    }

    public static async Task<long?> ResolveProjectIdAsync(
        ProjectContextResolver resolver,
        long? projectId,
        string? workspacePath)
    {
        if (projectId.HasValue && projectId.Value > 0)
            return projectId.Value;

        var resolvedProjectId = await ResolveProjectIdAsync(resolver, 0, workspacePath);
        return resolvedProjectId > 0 ? resolvedProjectId : null;
    }
}
