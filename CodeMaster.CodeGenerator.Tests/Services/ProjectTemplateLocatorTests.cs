using System.IO.Compression;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class ProjectTemplateLocatorTests
{
    [Fact]
    public void ValidateTemplateZip_RejectsHistoricalMigrations()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_dirty.zip");
            CreateTemplateZip(templatePath, includeMigration: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("historical EF migration", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsDanglingMcpTokenReferences()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_mcp.zip");
            CreateTemplateZip(templatePath, includeMigration: false, includeMcpReference: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("MCP token registrations", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsDanglingFrontendMcpReferences()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_frontend_mcp.zip");
            CreateTemplateZip(
                templatePath,
                includeMigration: false,
                includeFrontendMcpReference: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("MCP frontend reference", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsDanglingFrontendAgentReferences()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_frontend_agent.zip");
            CreateTemplateZip(
                templatePath,
                includeMigration: false,
                includeFrontendAgentReference: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("Agent frontend reference", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsMissingRequiredFrameworkFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_incomplete.zip");
            CreateTemplateZip(templatePath, includeMigration: false, includeRequiredFrameworkFiles: false);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("missing required framework files", error.Message);
            Assert.Contains("IQueryApplicationService.cs", error.Message);
            Assert.Contains("ITree.cs", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsAgentDbContextReferences()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_agent.zip");
            CreateTemplateZip(templatePath, includeMigration: false, includeAgentDbContextReference: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("Agent entity references", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsSolutionReferencesToMissingProjects()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_dangling_solution.zip");
            CreateTemplateZip(templatePath, includeMigration: false, includeMissingSolutionProject: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("project files that are not included", error.Message);
            Assert.Contains("CodeMaster.Agent.csproj", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidateTemplateZip_RejectsProjectReferencesToMissingProjects()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "CodeMaster_Template_dangling_project_reference.zip");
            CreateTemplateZip(templatePath, includeMigration: false, includeMissingProjectReference: true);

            var error = Assert.Throws<Exception>(() =>
                ProjectTemplateLocator.ValidateTemplateZip(templatePath));

            Assert.Contains("reference projects that are not included", error.Message);
            Assert.Contains("CodeMaster.Agent.csproj", error.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void FindLatestValidTemplateZip_SkipsNewerNestedLegacyTemplate()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "CodeMaster.sln"), string.Empty);
            File.WriteAllText(Path.Combine(root, "template-config.json"), "{}");

            var canonicalDirectory = Path.Combine(root, "Templates");
            var nestedDirectory = Path.Combine(root, "CodeMaster.WebApi", "Templates");
            Directory.CreateDirectory(canonicalDirectory);
            Directory.CreateDirectory(nestedDirectory);

            var cleanTemplate = Path.Combine(canonicalDirectory, "CodeMaster_Template_clean.zip");
            var dirtyTemplate = Path.Combine(nestedDirectory, "CodeMaster_Template_dirty.zip");
            CreateTemplateZip(cleanTemplate, includeMigration: false);
            CreateTemplateZip(dirtyTemplate, includeMigration: true);
            File.SetLastWriteTimeUtc(cleanTemplate, DateTime.UtcNow.AddMinutes(-1));
            File.SetLastWriteTimeUtc(dirtyTemplate, DateTime.UtcNow);

            var selected = ProjectTemplateLocator.FindLatestValidTemplateZip(
                null,
                Path.Combine(root, "CodeMaster.WebApi"),
                Path.Combine(root, "CodeMaster.WebApi", "bin", "Debug", "net10.0"));

            Assert.Equal(cleanTemplate, selected);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ResolveWritableDirectory_UsesSolutionRootTemplates()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "CodeMaster.sln"), string.Empty);
            File.WriteAllText(Path.Combine(root, "template-config.json"), "{}");
            var nestedDirectory = Path.Combine(root, "CodeMaster.WebApi");
            Directory.CreateDirectory(Path.Combine(nestedDirectory, "Templates"));

            var selected = ProjectTemplateLocator.ResolveWritableDirectory(
                null,
                nestedDirectory,
                Path.Combine(nestedDirectory, "bin", "Debug", "net10.0"));

            Assert.Equal(Path.Combine(root, "Templates"), selected);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"codemaster-template-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateTemplateZip(
        string path,
        bool includeMigration,
        bool includeMcpReference = false,
        bool includeFrontendMcpReference = false,
        bool includeRequiredFrameworkFiles = true,
        bool includeAgentDbContextReference = false,
        bool includeMissingSolutionProject = false,
        bool includeMissingProjectReference = false,
        bool includeFrontendAgentReference = false)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(
            archive,
            "CodeMaster.sln",
            includeMissingSolutionProject
                ? "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"CodeMaster.Agent\", \"CodeMaster.Agent\\CodeMaster.Agent.csproj\", \"{B163E5F5-F7D2-4552-8BA9-CFE1BF3C7F86}\""
                : string.Empty);
        WriteEntry(
            archive,
            "CodeMaster.WebApi/Program.cs",
            includeMcpReference
                ? "builder.Services.AddScoped<IMcpTokenService, McpTokenService>();"
                : "app.UseAuthentication();");
        WriteEntry(
            archive,
            "CodeMaster.WebApi/CodeMaster.WebApi.csproj",
            includeMissingProjectReference
                ? "<Project><ItemGroup><ProjectReference Include=\"..\\CodeMaster.Agent\\CodeMaster.Agent.csproj\" /></ItemGroup></Project>"
                : "<Project />");
        WriteEntry(archive, "CodeMaster.Vue/package.json");
        WriteEntry(
            archive,
            "CodeMaster.Migrator/Persistence/EfCore/CodeMasterDbContext.cs",
            includeAgentDbContextReference
                ? "using CodeMaster.Domain.Entities.Ai; modelBuilder.Entity<AiMessage>();"
                : "using CodeMaster.Domain.Entities.System;");
        if (includeRequiredFrameworkFiles)
        {
            WriteEntry(archive, "CodeMaster.Core/Entities/ITree.cs");
            WriteEntry(archive, "CodeMaster.Core/Services/IQueryApplicationService.cs");
            WriteEntry(archive, "CodeMaster.Core/Services/IReadOnlyTreeApplicationService.cs");
            WriteEntry(archive, "CodeMaster.Application/Services/QueryApplicationService.cs");
            WriteEntry(archive, "CodeMaster.Application/Services/ReadOnlyTreeApplicationService.cs");
        }

        if (includeFrontendMcpReference)
        {
            WriteEntry(
                archive,
                "CodeMaster.Vue/src/router/index.js",
                "component: () => import('@/views/profile/mcpToken/index.vue')");
        }
        else if (includeFrontendAgentReference)
        {
            WriteEntry(
                archive,
                "CodeMaster.Vue/src/layout/index.vue",
                "import AgentDrawer from '@/components/AgentAssistant/AgentDrawer.vue'");
        }

        if (includeMigration)
            WriteEntry(archive, "CodeMaster.Migrator/Migrations/20260214144217_InitialCreate.cs");
    }

    private static void WriteEntry(ZipArchive archive, string entryName, string content = "test")
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
