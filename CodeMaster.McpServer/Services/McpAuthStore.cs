using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMaster.McpServer.Services;

public class McpAuthStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string AuthFilePath
    {
        get
        {
            var configuredPath = Environment.GetEnvironmentVariable("CODEMASTER_MCP_AUTH_FILE");
            return string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".codemaster",
                    "mcp-auth.json")
                : Path.GetFullPath(configuredPath.Trim());
        }
    }

    public async Task SaveAsync(McpAuthEntry entry)
    {
        var file = await ReadFileAsync();
        var normalizedBaseUrl = NormalizeBaseUrl(entry.ServerBaseUrl);
        file.Tokens.RemoveAll(x => NormalizeBaseUrl(x.ServerBaseUrl) == normalizedBaseUrl);
        entry.ServerBaseUrl = normalizedBaseUrl;
        file.Tokens.Add(entry);

        Directory.CreateDirectory(Path.GetDirectoryName(AuthFilePath)!);
        await File.WriteAllTextAsync(AuthFilePath, JsonSerializer.Serialize(file, JsonOptions), new UTF8Encoding(false));
        TryRestrictFilePermissions(AuthFilePath);
    }

    public async Task<McpAuthEntry?> GetAsync(string? serverBaseUrl)
    {
        var file = await ReadFileAsync();
        if (!string.IsNullOrWhiteSpace(serverBaseUrl))
        {
            var normalizedBaseUrl = NormalizeBaseUrl(serverBaseUrl);
            return file.Tokens.LastOrDefault(x => NormalizeBaseUrl(x.ServerBaseUrl) == normalizedBaseUrl);
        }

        return file.Tokens.LastOrDefault();
    }

    public async Task<bool> RemoveAsync(string? serverBaseUrl)
    {
        var file = await ReadFileAsync();
        var before = file.Tokens.Count;

        if (string.IsNullOrWhiteSpace(serverBaseUrl))
        {
            file.Tokens.Clear();
        }
        else
        {
            var normalizedBaseUrl = NormalizeBaseUrl(serverBaseUrl);
            file.Tokens.RemoveAll(x => NormalizeBaseUrl(x.ServerBaseUrl) == normalizedBaseUrl);
        }

        if (file.Tokens.Count == before)
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(AuthFilePath)!);
        await File.WriteAllTextAsync(AuthFilePath, JsonSerializer.Serialize(file, JsonOptions), new UTF8Encoding(false));
        TryRestrictFilePermissions(AuthFilePath);
        return true;
    }

    public async Task<McpWhoAmIResult> ValidateAsync(string serverBaseUrl, string token)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(NormalizeBaseUrl(serverBaseUrl))
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.GetAsync("/api/auth/info");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"CodeMaster auth failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");

        var apiResponse = JsonSerializer.Deserialize<ApiResponse<McpUserInfoResponse>>(body, JsonOptions);
        if (apiResponse?.Code != 200 || apiResponse.Data == null)
            throw new InvalidOperationException(apiResponse?.Message ?? "CodeMaster auth failed.");

        return new McpWhoAmIResult
        {
            ServerBaseUrl = NormalizeBaseUrl(serverBaseUrl),
            UserId = apiResponse.Data.User.UserId,
            UserName = apiResponse.Data.User.UserName,
            NickName = apiResponse.Data.User.NickName,
            IsAdmin = apiResponse.Data.IsAdmin,
            IsHostAdmin = apiResponse.Data.IsHostAdmin,
            IsTenantAdmin = apiResponse.Data.IsTenantAdmin,
            Roles = apiResponse.Data.Roles,
            Permissions = apiResponse.Data.Permissions
        };
    }

    public static string NormalizeBaseUrl(string? serverBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(serverBaseUrl))
            throw new ArgumentException("serverBaseUrl is required.");

        return serverBaseUrl.Trim().TrimEnd('/');
    }

    private async Task<McpAuthFile> ReadFileAsync()
    {
        if (!File.Exists(AuthFilePath))
            return new McpAuthFile();

        try
        {
            var json = await File.ReadAllTextAsync(AuthFilePath);
            return JsonSerializer.Deserialize<McpAuthFile>(json, JsonOptions) ?? new McpAuthFile();
        }
        catch
        {
            return new McpAuthFile();
        }
    }

    private static void TryRestrictFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch
        {
            // Best-effort only; token validation still happens on the server.
        }
    }
}

public class McpAuthFile
{
    public List<McpAuthEntry> Tokens { get; set; } = new();
}

public class McpAuthEntry
{
    public string ServerBaseUrl { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? NickName { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}

public class McpWhoAmIResult
{
    public string ServerBaseUrl { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }

    public List<string> Roles { get; set; } = new();

    public List<string> Permissions { get; set; } = new();
}

public class ApiResponse<T>
{
    public int Code { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }
}

public class McpUserInfoResponse
{
    public McpUserBasic User { get; set; } = new();

    public List<string> Roles { get; set; } = new();

    public List<string> Permissions { get; set; } = new();

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }
}

public class McpUserBasic
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? NickName { get; set; }
}
