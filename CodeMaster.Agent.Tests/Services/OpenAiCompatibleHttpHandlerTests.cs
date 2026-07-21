using CodeMaster.Agent.Services;

namespace CodeMaster.Agent.Tests.Services;

public class OpenAiCompatibleHttpHandlerTests
{
    [Fact]
    public void ApplyHeaders_ReplacesSdkUserAgentAndKeepsAuthorization()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/v1/chat/completions");
        request.Headers.TryAddWithoutValidation("User-Agent", "OpenAI/2.10.0 MEAI/10.6.0");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer original-key");

        var headers = OpenAiCompatibleHttpHandler.ParseExtraHeaders(
            "{\"X-Provider-Route\":\"primary\",\"Authorization\":\"Bearer replacement\"}");
        OpenAiCompatibleHttpHandler.ApplyHeaders(request, headers);

        Assert.Equal("CodeMaster/1.0", string.Join(" ", request.Headers.GetValues("User-Agent")));
        Assert.Equal("Bearer original-key", string.Join(" ", request.Headers.GetValues("Authorization")));
        Assert.Equal("primary", string.Join(" ", request.Headers.GetValues("X-Provider-Route")));
    }

    [Fact]
    public void ParseExtraHeaders_RejectsNestedValues()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            OpenAiCompatibleHttpHandler.ParseExtraHeaders("{\"X-Options\":{\"region\":\"cn\"}}"));

        Assert.Contains("must be a scalar value", exception.Message);
    }
}
