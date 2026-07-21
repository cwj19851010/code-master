using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMaster.McpServer.Services;

public sealed class CodeMasterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly McpSessionResolver _sessionResolver;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CodeMasterApiClient(HttpClient httpClient, McpSessionResolver sessionResolver)
    {
        _httpClient = httpClient;
        _sessionResolver = sessionResolver;
    }

    public Task<T> GetAsync<T>(string path, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, path, null, workspacePath, cancellationToken);
    }

    public Task<T> PostAsync<T>(string path, object? body, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, path, body, workspacePath, cancellationToken);
    }

    public Task<T> PutAsync<T>(string path, object? body, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Put, path, body, workspacePath, cancellationToken);
    }

    public async Task<JsonElement> GetJsonAsync(string path, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return await SendAsync<JsonElement>(HttpMethod.Get, path, null, workspacePath, cancellationToken);
    }

    public async Task<JsonElement> PostJsonAsync(string path, object? body, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return await SendAsync<JsonElement>(HttpMethod.Post, path, body, workspacePath, cancellationToken);
    }

    public async Task<JsonElement> PutJsonAsync(string path, object? body, string? workspacePath = null, CancellationToken cancellationToken = default)
    {
        return await SendAsync<JsonElement>(HttpMethod.Put, path, body, workspacePath, cancellationToken);
    }

    public Task<McpSession> ResolveSessionAsync(string? workspacePath = null)
    {
        return _sessionResolver.ResolveAsync(workspacePath);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        var session = await _sessionResolver.ResolveAsync(workspacePath);
        using var request = new HttpRequestMessage(method, BuildUri(session.ServerBaseUrl, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "CodeMaster MCP authentication failed. The saved token is missing, expired, or revoked; call codemaster_login again.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "CodeMaster denied this operation. Check the current user's tenant and code-generation permissions.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"CodeMaster API request failed: {(int)response.StatusCode} {response.ReasonPhrase} {ExtractErrorMessage(content)}");
        }

        return DeserializeResponse<T>(content, path);
    }

    private T DeserializeResponse<T>(string content, string path)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)true;

            throw new InvalidOperationException($"CodeMaster API returned an empty response: {path}");
        }

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("code", out var codeElement))
        {
            var code = codeElement.TryGetInt32(out var parsedCode) ? parsedCode : 0;
            if (code != 200)
            {
                var message = root.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : null;
                throw new InvalidOperationException(message ?? $"CodeMaster API returned code {code}: {path}");
            }

            if (!root.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (typeof(T) == typeof(bool))
                    return (T)(object)true;

                throw new InvalidOperationException($"CodeMaster API returned no data: {path}");
            }

            return dataElement.Deserialize<T>(_jsonOptions)
                ?? throw new InvalidOperationException($"CodeMaster API data could not be deserialized: {path}");
        }

        return root.Deserialize<T>(_jsonOptions)
            ?? throw new InvalidOperationException($"CodeMaster API response could not be deserialized: {path}");
    }

    private static Uri BuildUri(string serverBaseUrl, string path)
    {
        return new Uri($"{serverBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    private static string ExtractErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? content;
        }
        catch
        {
        }

        const int maxLength = 2000;
        return content.Length <= maxLength ? content : content[..maxLength];
    }
}
