namespace CodeMaster.McpServer.Services;

public sealed class McpSessionResolver
{
    private readonly McpAuthStore _authStore;
    private readonly ProjectContextResolver _contextResolver;

    public McpSessionResolver(McpAuthStore authStore, ProjectContextResolver contextResolver)
    {
        _authStore = authStore;
        _contextResolver = contextResolver;
    }

    public async Task<McpSession> ResolveAsync(string? workspacePath = null, bool requireAuthentication = true)
    {
        var projectContext = await _contextResolver.ResolveAsync(workspacePath);
        var auth = await _authStore.GetAsync(projectContext?.ServerBaseUrl);

        if (auth == null && !string.IsNullOrWhiteSpace(projectContext?.ServerBaseUrl))
        {
            auth = await _authStore.GetAsync(null);
        }

        if (auth == null)
        {
            if (requireAuthentication)
            {
                throw new InvalidOperationException(
                    "No CodeMaster MCP token is saved. Generate a token in CodeMaster personal center and call codemaster_login first.");
            }

            var serverBaseUrl = projectContext?.ServerBaseUrl;
            if (string.IsNullOrWhiteSpace(serverBaseUrl))
            {
                throw new InvalidOperationException(
                    "CodeMaster server address was not found. Open a generated project containing .codemaster/project-context.json or call codemaster_login.");
            }

            return new McpSession(McpAuthStore.NormalizeBaseUrl(serverBaseUrl), null, projectContext);
        }

        var contextBaseUrl = projectContext?.ServerBaseUrl;
        var server = string.IsNullOrWhiteSpace(contextBaseUrl)
            ? auth.ServerBaseUrl
            : McpAuthStore.NormalizeBaseUrl(contextBaseUrl);

        if (!string.Equals(server, McpAuthStore.NormalizeBaseUrl(auth.ServerBaseUrl), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"No MCP token is saved for {server}. Call codemaster_login for this CodeMaster server first.");
        }

        return new McpSession(server, auth.Token, projectContext);
    }
}

public sealed record McpSession(
    string ServerBaseUrl,
    string? AccessToken,
    ProjectContextInfo? ProjectContext);
