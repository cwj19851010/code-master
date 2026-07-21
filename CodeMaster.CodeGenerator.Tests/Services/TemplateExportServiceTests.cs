using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class TemplateExportServiceTests
{
    [Fact]
    public async Task RemoveCodeGenLinesAsync_RemovesMcpRegistrationsAndMiddleware()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-template-export-tests-{Guid.NewGuid():N}");
        var webApiDirectory = Path.Combine(root, "CodeMaster.WebApi");
        var programPath = Path.Combine(webApiDirectory, "Program.cs");

        try
        {
            Directory.CreateDirectory(webApiDirectory);
            await File.WriteAllLinesAsync(programPath,
            [
                "builder.Services.AddScoped<CodeMaster.Application.Services.Auth.IAuthService, CodeMaster.Application.Services.Auth.AuthService>();",
                "builder.Services.AddScoped<CodeMaster.Application.Services.Auth.IMcpTokenService, CodeMaster.Application.Services.Auth.McpTokenService>();",
                "// MCP tokens are validated before JWT authentication.",
                "app.UseMiddleware<CodeMaster.WebApi.Middleware.McpTokenAuthenticationMiddleware>();",
                "app.UseAuthentication();"
            ]);

            var service = new TemplateExportService();
            await service.RemoveCodeGenLinesAsync(root);

            var result = await File.ReadAllTextAsync(programPath);
            Assert.DoesNotContain("McpToken", result);
            Assert.Contains("IAuthService", result);
            Assert.Contains("app.UseAuthentication();", result);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RemoveFrontendMcpReferencesAsync_RemovesRouteAndMenuItem()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-template-export-tests-{Guid.NewGuid():N}");
        var routerDirectory = Path.Combine(root, "CodeMaster.Vue", "src", "router");
        var layoutDirectory = Path.Combine(root, "CodeMaster.Vue", "src", "layout");
        var routerPath = Path.Combine(routerDirectory, "index.js");
        var layoutPath = Path.Combine(layoutDirectory, "index.vue");

        try
        {
            Directory.CreateDirectory(routerDirectory);
            Directory.CreateDirectory(layoutDirectory);
            await File.WriteAllLinesAsync(routerPath,
            [
                "children: [",
                "  {",
                "    path: 'dashboard',",
                "    name: 'Dashboard'",
                "  },",
                "  {",
                "    path: 'profile/mcp-token',",
                "    name: 'McpToken',",
                "    component: () => import('@/views/profile/mcpToken/index.vue'),",
                "    meta: { title: 'MCP Token', hidden: true }",
                "  },",
                "]"
            ]);
            await File.WriteAllLinesAsync(layoutPath,
            [
                "<el-dropdown-menu>",
                "  <el-dropdown-item @click=\"router.push('/profile/mcp-token')\">MCP Token</el-dropdown-item>",
                "  <el-dropdown-item divided @click=\"handleLogout\">Logout</el-dropdown-item>",
                "</el-dropdown-menu>"
            ]);

            var service = new TemplateExportService();
            await service.RemoveFrontendMcpReferencesAsync(root);

            var routerResult = await File.ReadAllTextAsync(routerPath);
            var layoutResult = await File.ReadAllTextAsync(layoutPath);
            Assert.DoesNotContain("mcp-token", routerResult, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mcpToken", routerResult, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Dashboard", routerResult);
            Assert.DoesNotContain("mcp-token", layoutResult, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<el-dropdown-item @click=\"handleLogout\">", layoutResult);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
