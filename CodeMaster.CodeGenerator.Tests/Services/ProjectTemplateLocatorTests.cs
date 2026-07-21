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
        bool includeFrontendMcpReference = false)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(archive, "CodeMaster.sln");
        WriteEntry(
            archive,
            "CodeMaster.WebApi/Program.cs",
            includeMcpReference
                ? "builder.Services.AddScoped<IMcpTokenService, McpTokenService>();"
                : "app.UseAuthentication();");
        WriteEntry(archive, "CodeMaster.Vue/package.json");
        if (includeFrontendMcpReference)
        {
            WriteEntry(
                archive,
                "CodeMaster.Vue/src/router/index.js",
                "component: () => import('@/views/profile/mcpToken/index.vue')");
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
