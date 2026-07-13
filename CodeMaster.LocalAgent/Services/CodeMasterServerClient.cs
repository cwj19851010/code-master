using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Common;

namespace CodeMaster.LocalAgent.Services;

public class CodeMasterServerClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public CodeMasterServerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<GenerationBundleDto> GetGenerationBundleAsync(string serverBaseUrl, long projectId, string? token)
    {
        return GetApiDataAsync<GenerationBundleDto>(
            serverBaseUrl,
            $"/api/codegen/project/{projectId}/generation-bundle",
            token);
    }

    public Task<ClientInitializeProjectDto> GetClientInitializeDataAsync(string serverBaseUrl, long projectId, string? token)
    {
        return GetApiDataAsync<ClientInitializeProjectDto>(
            serverBaseUrl,
            $"/api/codegen/project/{projectId}/client-init-data",
            token);
    }

    public Task<ModuleEntityDto> GetModuleEntityAsync(string serverBaseUrl, long entityId, string? token)
    {
        return GetApiDataAsync<ModuleEntityDto>(
            serverBaseUrl,
            $"/api/codegen/moduleentity/getbyid/{entityId}",
            token);
    }

    public Task<ProjectModuleDto> GetProjectModuleAsync(string serverBaseUrl, long moduleId, string? token)
    {
        return GetApiDataAsync<ProjectModuleDto>(
            serverBaseUrl,
            $"/api/codegen/projectmodule/getbyid/{moduleId}",
            token);
    }

    public async Task<(string FileName, byte[] Content)> DownloadTemplateAsync(string serverBaseUrl, string? token)
    {
        if (string.IsNullOrWhiteSpace(serverBaseUrl))
            throw new InvalidOperationException("serverBaseUrl is required");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(serverBaseUrl, "/api/codegen/project/template/download"));

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var content = await response.Content.ReadAsByteArrayAsync();
        if (!response.IsSuccessStatusCode)
        {
            var errorText = Encoding.UTF8.GetString(content);
            throw new InvalidOperationException($"Server request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{errorText}");
        }

        if (content.Length == 0)
            throw new InvalidOperationException("Server returned an empty template file");

        return (GetDownloadFileName(response), content);
    }

    public async Task CompleteLocalExecutionAsync(string serverBaseUrl, LocalExecutionCompleteDto input, string? token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(serverBaseUrl, "/api/codegen/project/local-execution/completed"));

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(input, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Server callback failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{content}");
        }

        var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
        if (apiResponse == null)
        {
            throw new InvalidOperationException("Server callback returned an empty response");
        }

        if (apiResponse.Code != 200)
        {
            throw new InvalidOperationException(apiResponse.Message);
        }
    }

    private async Task<T> GetApiDataAsync<T>(string serverBaseUrl, string path, string? token)
    {
        if (string.IsNullOrWhiteSpace(serverBaseUrl))
            throw new InvalidOperationException("serverBaseUrl is required");

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(serverBaseUrl, path));
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Server request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{content}");
        }

        return DeserializeApiData<T>(content, path);
    }

    private static Uri BuildUri(string serverBaseUrl, string path)
    {
        return new Uri($"{serverBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    private static string GetDownloadFileName(HttpResponseMessage response)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;
        var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        fileName = fileName?.Trim('"');

        return string.IsNullOrWhiteSpace(fileName)
            ? $"CodeMaster_Template_{DateTime.UtcNow:yyyyMMddHHmmssfff}.zip"
            : Path.GetFileName(fileName);
    }

    private T DeserializeApiData<T>(string content, string path)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException($"Server returned an empty response: {path}");

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (IsApiResponse(root))
        {
            var code = root.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsedCode)
                ? parsedCode
                : 0;

            if (code != 200)
            {
                var message = TryGetString(root, "message");
                if (string.IsNullOrWhiteSpace(message))
                    message = $"Server API returned code {code}: {path}";

                throw new InvalidOperationException(message);
            }

            if (!root.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new InvalidOperationException($"Server returned no data: {path}");
            }

            return dataElement.Deserialize<T>(_jsonOptions)
                ?? throw new InvalidOperationException($"Server data could not be deserialized: {path}");
        }

        return root.Deserialize<T>(_jsonOptions)
            ?? throw new InvalidOperationException($"Server response could not be deserialized: {path}");
    }

    private static bool IsApiResponse(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty("code", out _) &&
               (root.TryGetProperty("data", out _) || root.TryGetProperty("message", out _));
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
