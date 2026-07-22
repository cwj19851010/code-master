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

    [Fact]
    public async Task RemoveAgentDbContextReferencesAsync_RemovesCodeMasterAgentEntities()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-template-export-tests-{Guid.NewGuid():N}");
        var dbContextDirectory = Path.Combine(root, "CodeMaster.Migrator", "Persistence", "EfCore");
        var dbContextPath = Path.Combine(dbContextDirectory, "CodeMasterDbContext.cs");

        try
        {
            Directory.CreateDirectory(dbContextDirectory);
            await File.WriteAllLinesAsync(dbContextPath,
            [
                "using Microsoft.EntityFrameworkCore;",
                "using CodeMaster.Domain.Entities.Ai;",
                "using CodeMaster.Domain.Entities.System;",
                "",
                "protected override void OnModelCreating(ModelBuilder modelBuilder)",
                "{",
                "    modelBuilder.Entity<AiMessage>()",
                "        .Property(x => x.RequestId)",
                "        .HasMaxLength(64);",
                "    modelBuilder.Entity<AiToolExecution>()",
                "        .HasIndex(x => x.RequestId)",
                "        .IsUnique();",
                "    modelBuilder.Entity<SysUser>();",
                "}"
            ]);

            var service = new TemplateExportService();
            await service.RemoveAgentDbContextReferencesAsync(root);

            var result = await File.ReadAllTextAsync(dbContextPath);
            Assert.DoesNotContain("Domain.Entities.Ai", result);
            Assert.DoesNotContain("Entity<AiMessage>", result);
            Assert.DoesNotContain("Entity<AiToolExecution>", result);
            Assert.Contains("Entity<SysUser>", result);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RemovePlatformWebApiProjectReferencesAsync_RemovesPlatformOnlyReferences()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-template-export-tests-{Guid.NewGuid():N}");
        var webApiDirectory = Path.Combine(root, "CodeMaster.WebApi");
        var projectPath = Path.Combine(webApiDirectory, "CodeMaster.WebApi.csproj");

        try
        {
            Directory.CreateDirectory(webApiDirectory);
            await File.WriteAllTextAsync(projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <ItemGroup>
                    <ProjectReference Include="..\CodeMaster.Agent\CodeMaster.Agent.csproj" />
                    <ProjectReference Include="..\CodeMaster.Application\CodeMaster.Application.csproj" />
                  </ItemGroup>
                  <ItemGroup>
                    <Content Include="..\Templates\CodeMaster_Template_*.zip" />
                    <Content Include="..\CodeMaster.CodeGenerator\Templates\**\*.*" />
                  </ItemGroup>
                </Project>
                """);

            var service = new TemplateExportService();
            await service.RemovePlatformWebApiProjectReferencesAsync(root);

            var result = await File.ReadAllTextAsync(projectPath);
            Assert.DoesNotContain("CodeMaster.Agent", result);
            Assert.DoesNotContain("CodeMaster_Template_", result);
            Assert.DoesNotContain("CodeMaster.CodeGenerator", result);
            Assert.Contains("CodeMaster.Application", result);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RemoveFrontendAgentReferencesAsync_RemovesAgentUiAndState()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-template-export-tests-{Guid.NewGuid():N}");
        var layoutDirectory = Path.Combine(root, "CodeMaster.Vue", "src", "layout");
        var layoutPath = Path.Combine(layoutDirectory, "index.vue");

        try
        {
            Directory.CreateDirectory(layoutDirectory);
            await File.WriteAllTextAsync(layoutPath,
                """
                <template>
                  <el-tooltip content="AI 助手" placement="bottom">
                    <el-button :icon="ChatDotRound" @click="agentVisible = true" />
                  </el-tooltip>
                  <theme-picker />
                  <agent-drawer v-model="agentVisible" />
                </template>
                <script setup>
                import { Fold, Expand, ChatDotRound } from '@element-plus/icons-vue'
                import AgentDrawer from '@/components/AgentAssistant/AgentDrawer.vue'
                const agentVisible = ref(false)
                </script>
                <style scoped>
                .agent-trigger {
                  color: red;
                  &:hover {
                    color: blue;
                  }
                }
                .header-item { display: flex; }
                </style>
                """);

            var service = new TemplateExportService();
            await service.RemoveFrontendAgentReferencesAsync(root);

            var result = await File.ReadAllTextAsync(layoutPath);
            Assert.DoesNotContain("AgentAssistant", result);
            Assert.DoesNotContain("agentVisible", result);
            Assert.DoesNotContain("ChatDotRound", result);
            Assert.DoesNotContain("agent-trigger", result);
            Assert.Contains("theme-picker", result);
            Assert.Contains("header-item", result);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
