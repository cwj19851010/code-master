using System.Text.Json;

namespace CodeMaster.Agent.Services;

internal sealed class OpenAiCompatibleHttpHandler : HttpMessageHandler
{
    private const string DefaultUserAgent = "CodeMaster/1.0";
    private static readonly HttpMessageInvoker SharedInvoker = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
    });

    private readonly IReadOnlyDictionary<string, string> _extraHeaders;

    public OpenAiCompatibleHttpHandler(string? extraHeadersJson)
    {
        _extraHeaders = ParseExtraHeaders(extraHeadersJson);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ApplyHeaders(request, _extraHeaders);
        return SharedInvoker.SendAsync(request, cancellationToken);
    }

    internal static IReadOnlyDictionary<string, string> ParseExtraHeaders(string? extraHeadersJson)
    {
        if (string.IsNullOrWhiteSpace(extraHeadersJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(extraHeadersJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Extra headers must be a JSON object.");
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name)) continue;
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    throw new ArgumentException($"Extra header '{property.Name}' must be a scalar value.");
                }

                headers[property.Name.Trim()] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            return headers;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Extra headers must be valid JSON.", ex);
        }
    }

    internal static void ApplyHeaders(
        HttpRequestMessage request,
        IReadOnlyDictionary<string, string> extraHeaders)
    {
        var userAgent = extraHeaders.TryGetValue("User-Agent", out var configuredUserAgent) &&
                        !string.IsNullOrWhiteSpace(configuredUserAgent)
            ? configuredUserAgent
            : DefaultUserAgent;

        // Some OpenAI-compatible gateways reject the OpenAI .NET SDK user-agent.
        request.Headers.Remove("User-Agent");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        foreach (var (name, value) in extraHeaders)
        {
            if (string.Equals(name, "User-Agent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.Remove(name);
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }
}
