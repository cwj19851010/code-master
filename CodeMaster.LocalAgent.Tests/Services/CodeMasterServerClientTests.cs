using System.Net;
using CodeMaster.LocalAgent.Services;

namespace CodeMaster.LocalAgent.Tests.Services;

public class CodeMasterServerClientTests
{
    [Fact]
    public async Task GetModuleEntityAsync_accepts_raw_dto_with_long_values_as_strings()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "id": "7086015366884709802",
              "projectId": "1",
              "moduleId": "2",
              "name": "Order",
              "description": "Order",
              "hasPrimaryKey": true,
              "generateFrontend": true
            }
            """));
        var client = new CodeMasterServerClient(new HttpClient(handler));

        var entity = await client.GetModuleEntityAsync(
            "http://codemaster.test",
            7086015366884709802,
            "server-token");

        Assert.Equal(7086015366884709802, entity.Id);
        Assert.Equal(1, entity.ProjectId);
        Assert.Equal(2, entity.ModuleId);
        Assert.Equal("Order", entity.Name);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("server-token", handler.AuthorizationParameter);
        Assert.Equal(
            "http://codemaster.test/api/codegen/moduleentity/getbyid/7086015366884709802",
            handler.RequestUri);
    }

    [Fact]
    public async Task GetGenerationBundleAsync_unwraps_api_response_data()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "code": 200,
              "message": "ok",
              "data": {
                "version": 1,
                "generatedAt": "2026-06-27T00:00:00Z",
                "project": {
                  "id": "9007199254740993",
                  "projectName": "OrderManager",
                  "displayName": "Order Manager",
                  "databaseType": 4,
                  "connectionString": "Data Source=OrderManager.db",
                  "projectPath": "D:\\Projects",
                  "projectType": 1,
                  "status": 1,
                  "createTime": "2026-06-27T00:00:00Z"
                },
                "modules": [
                  {
                    "id": "11",
                    "projectId": "9007199254740993",
                    "moduleName": "Sales",
                    "moduleDescription": "Sales",
                    "orderNum": 1,
                    "createTime": "2026-06-27T00:00:00Z"
                  }
                ],
                "entities": [],
                "fields": [],
                "relations": [],
                "pageTemplates": [],
                "fieldControlTemplates": [],
                "childTemplates": []
              }
            }
            """));
        var client = new CodeMasterServerClient(new HttpClient(handler));

        var bundle = await client.GetGenerationBundleAsync(
            "http://codemaster.test/",
            9007199254740993,
            null);

        Assert.Equal(1, bundle.Version);
        Assert.Equal(9007199254740993, bundle.Project.Id);
        Assert.Equal("OrderManager", bundle.Project.ProjectName);
        Assert.Single(bundle.Modules);
        Assert.Equal(11, bundle.Modules[0].Id);
        Assert.Equal("http://codemaster.test/api/codegen/project/9007199254740993/generation-bundle", handler.RequestUri);
    }

    [Fact]
    public async Task GetGenerationBundleAsync_throws_server_message_for_non_success_api_code()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "code": 500,
              "message": "bundle failed",
              "data": null
            }
            """));
        var client = new CodeMasterServerClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetGenerationBundleAsync("http://codemaster.test", 1, null));

        Assert.Equal("bundle failed", exception.Message);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public string? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(_handler(request));
        }
    }
}
