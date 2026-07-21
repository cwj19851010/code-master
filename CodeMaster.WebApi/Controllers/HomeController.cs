using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CodeMaster.Application.Dtos.Community;
using CodeMaster.Application.Services.Community;
using CodeMaster.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

public class HomeController : Controller
{
    private const string WindowsClientInstallerRelativeDirectory = "downloads/client/windows";
    private static readonly Regex WindowsClientInstallerNameRegex = new(
        @"^CodeMaster_v(?<version>\d+(?:\.\d+){1,3})\.exe$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ICommunityService _communityService;

    public HomeController(IConfiguration configuration, IWebHostEnvironment environment, ICommunityService communityService)
    {
        _configuration = configuration;
        _environment = environment;
        _communityService = communityService;
    }

    [HttpGet("/")]
    [HttpGet("/home")]
    [HttpGet("/home/index")]
    public IActionResult Index()
    {
        SetClientDownloadViewData();
        return View();
    }

    [HttpGet("/download")]
    public IActionResult Download()
    {
        SetClientDownloadViewData();
        return View();
    }

    [HttpGet("/download/client/windows/latest")]
    public IActionResult DownloadLatestWindowsClient()
    {
        var installer = FindLatestWindowsClientInstaller();
        if (installer == null)
        {
            return NotFound("No CodeMaster Windows client installer was found. Upload files like CodeMaster_v0.1.1.exe to wwwroot/downloads/client/windows.");
        }

        return PhysicalFile(installer.FileInfo.FullName, "application/octet-stream", installer.FileInfo.Name);
    }

    [HttpGet("/download/client/windows/{fileName}")]
    public IActionResult DownloadWindowsClient(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal) ||
            TryCreateWindowsClientInstallerCandidate(Path.Combine(GetWindowsClientInstallerDirectory(), safeFileName)) == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(GetWindowsClientInstallerDirectory(), safeFileName);
        if (!global::System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "application/octet-stream", safeFileName);
    }

    [HttpGet("/download/client/latest.json")]
    [HttpGet("/download/client/windows/latest.json")]
    public IActionResult GetLatestClientManifest()
    {
        var installer = FindLatestWindowsClientInstaller();
        if (installer == null)
        {
            return NotFound(new
            {
                message = "No CodeMaster Windows client installer was found.",
                expectedDirectory = "wwwroot/downloads/client/windows",
                expectedFileName = "CodeMaster_v0.1.1.exe"
            });
        }

        return Json(new
        {
            version = installer.VersionText,
            platform = "windows-x64",
            url = BuildAbsoluteUrl("/download/client/windows/latest"),
            directUrl = BuildAbsoluteUrl($"/download/client/windows/{Uri.EscapeDataString(installer.FileInfo.Name)}"),
            fileName = installer.FileInfo.Name,
            size = installer.FileInfo.Length,
            publishedAt = installer.FileInfo.LastWriteTimeUtc
        });
    }

    [HttpGet("/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        var loginUrl = GetAdminUrl("Portal:AdminLoginUrl", "/login");
        return Redirect(loginUrl);
    }

    [HttpGet("/app")]
    public IActionResult App()
    {
        var appUrl = GetAdminUrl("Portal:AdminAppUrl", "/login");
        return Redirect(appUrl);
    }

    [HttpGet("/community")]
    public async Task<IActionResult> Community([FromQuery] long? categoryId, [FromQuery] string? keyword, [FromQuery] int pageNum = 1)
    {
        var categories = await _communityService.GetCategoriesAsync();
        var topics = await _communityService.GetTopicsAsync(new CommunityTopicQueryDto
        {
            CategoryId = categoryId,
            Keyword = keyword,
            PageNum = Math.Max(pageNum, 1),
            PageSize = 20
        });

        return View(new CommunityIndexViewModel
        {
            Categories = categories,
            Topics = topics,
            CategoryId = categoryId,
            Keyword = keyword
        });
    }

    [HttpGet("/community/topic/{id:long}")]
    public async Task<IActionResult> CommunityTopic(long id)
    {
        var topic = await _communityService.GetTopicAsync(id);
        if (topic == null)
        {
            return NotFound();
        }

        var replies = await _communityService.GetRepliesAsync(id);
        return View(new CommunityTopicViewModel
        {
            Topic = topic,
            Replies = replies
        });
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Content($"""
            User-agent: *
            Allow: /

            Sitemap: {baseUrl}/sitemap.xml
            """, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Content($"""
            <?xml version="1.0" encoding="UTF-8"?>
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url><loc>{baseUrl}/</loc><priority>1.0</priority></url>
              <url><loc>{baseUrl}/download</loc><priority>0.8</priority></url>
              <url><loc>{baseUrl}/register</loc><priority>0.7</priority></url>
              <url><loc>{baseUrl}/community</loc><priority>0.7</priority></url>
            </urlset>
            """, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/llms.txt")]
    public IActionResult Llms()
    {
        return Content("""
            # CodeMaster

            CodeMaster is a convention-driven .NET 10 and Vue 3 enterprise development platform. It turns project, module, entity, field, relation, and UI-control metadata into dynamic APIs, permissions, database migrations, seed data, frontend pages, backend services, and independently deployable source code.

            Important pages:
            - /: product overview
            - /download: client and source download
            - /register: email registration and GitHub login
            - /community: lightweight user forum

            Key concepts:
            - Convention over configuration: application services, method names, attributes, entity base types, and field metadata drive standard framework behavior.
            - Dynamic API: IApplicationService implementations are discovered automatically and exposed as REST endpoints with convention-based routes, HTTP methods, and parameter binding.
            - Authorization: menus, page permissions, operation permissions, tenant visibility, and backend permission policies use the same permission model.
            - Database lifecycle: SqlSugar is the runtime ORM; the standalone EF Core Migrator creates migrations, applies schema updates, and initializes seed data.
            - Frontend: Vue 3, Element Plus, Vite.
            - Clean template export: CodeMaster can export a complete generated-project template from the current framework source while excluding platform metadata, temporary output, and historical migrations.
            - Code generation: full and incremental generation cover domain entities, DTOs, services, API clients, query lists, add/edit pages, detail pages, menus, translations, field ordering, child tables, and structured entity relations.
            - Generated projects: output projects are independent .NET and Vue applications and do not require CodeMaster metadata services at runtime.
            - Visual design: the page designer supports Element Plus components, drag and drop, grouping, responsive grid layout, visual properties, and control-level ScriptSection bindings. Stable genId values and replayable design overlays preserve approved UI changes across generation.
            - External AI integration: CodeMaster MCP exposes controlled project, module, entity, field, relation, initialization, service-start, migration, generation, and build operations to Codex, Claude Code, and other MCP clients.
            - Built-in Agent: conversations bind to an explicit project. The Agent reads the project blueprint, proposes approval-based metadata or UI changes, executes them through the shared Web/Tauri path, and performs real build validation after generation.
            - AI providers: the built-in Agent supports OpenAI-compatible and Anthropic Claude server providers. Local execution is designed for the Tauri LocalAgent path and remains under active development.
            - Local capability: the Tauri client can load a remote Vue application while a trusted .NET LocalAgent sidecar handles local templates, paths, processes, and frontend/backend startup.
            - Deployment: CodeMaster supports private deployment. Platform services, frontend, database, templates, AI provider configuration, and generated projects can remain in infrastructure controlled by the user.
            - Enterprise foundation: multi-tenancy, users, roles, departments, posts, dictionaries, localization, data permissions, logs, scheduled tasks, online users, and SignalR notifications.
            - Coming next: UniApp, mini-program, and mobile page generation are planned for a future version and are not currently available.
            """, "text/plain", Encoding.UTF8);
    }

    private string GetAdminUrl(string key, string hashPath)
    {
        var configuredUrl = _configuration[key];
        var url = string.IsNullOrWhiteSpace(configuredUrl)
            ? (_environment.IsDevelopment() ? "http://localhost:5173" : "/index.html")
            : configuredUrl.Trim();

        return AddHashPath(url, hashPath);
    }

    private static string AddHashPath(string url, string hashPath)
    {
        if (url.Contains('#'))
        {
            return url;
        }

        var normalizedHashPath = hashPath.StartsWith('/') ? hashPath : $"/{hashPath}";
        return url.EndsWith('/')
            ? $"{url}#{normalizedHashPath}"
            : $"{url}#{normalizedHashPath}";
    }

    private ClientInstallerCandidate? FindLatestWindowsClientInstaller()
    {
        var directory = GetWindowsClientInstallerDirectory();
        if (!Directory.Exists(directory))
        {
            return null;
        }

        ClientInstallerCandidate? latest = null;
        foreach (var filePath in Directory.EnumerateFiles(directory, "CodeMaster_v*.exe", SearchOption.TopDirectoryOnly))
        {
            var candidate = TryCreateWindowsClientInstallerCandidate(filePath);
            if (candidate == null)
            {
                continue;
            }

            if (latest == null ||
                CompareVersionParts(candidate.VersionParts, latest.VersionParts) > 0 ||
                (CompareVersionParts(candidate.VersionParts, latest.VersionParts) == 0 &&
                 candidate.FileInfo.LastWriteTimeUtc > latest.FileInfo.LastWriteTimeUtc))
            {
                latest = candidate;
            }
        }

        return latest;
    }

    private string GetWindowsClientInstallerDirectory()
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(
            webRootPath,
            WindowsClientInstallerRelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
    }

    private static ClientInstallerCandidate? TryCreateWindowsClientInstallerCandidate(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var match = WindowsClientInstallerNameRegex.Match(fileName);
        if (!match.Success)
        {
            return null;
        }

        var versionText = match.Groups["version"].Value;
        var versionParts = ParseVersionParts(versionText);
        if (versionParts == null)
        {
            return null;
        }

        return new ClientInstallerCandidate(new FileInfo(filePath), versionText, versionParts);
    }

    private static int[]? ParseVersionParts(string versionText)
    {
        var rawParts = versionText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (rawParts.Length is < 2 or > 4)
        {
            return null;
        }

        var versionParts = new[] { 0, 0, 0, 0 };
        for (var i = 0; i < rawParts.Length; i++)
        {
            if (!int.TryParse(rawParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var part) || part < 0)
            {
                return null;
            }

            versionParts[i] = part;
        }

        return versionParts;
    }

    private static int CompareVersionParts(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        for (var i = 0; i < 4; i++)
        {
            var comparison = left[i].CompareTo(right[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private string BuildAbsoluteUrl(string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{normalizedPath}";
    }

    private void SetClientDownloadViewData()
    {
        var installer = FindLatestWindowsClientInstaller();
        ViewData["ClientDownloadUrl"] = "/download/client/windows/latest";
        ViewData["ClientDownloadManifestUrl"] = "/download/client/windows/latest.json";
        ViewData["ClientDownloadVersion"] = installer?.VersionText;
        ViewData["ClientDownloadFileName"] = installer?.FileInfo.Name;
        ViewData["ClientDownloadSize"] = installer == null ? null : FormatFileSize(installer.FileInfo.Length);
        ViewData["ClientDownloadPublishedAt"] = installer?.FileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        var size = bytes / 1024d;
        string[] units = ["KB", "MB", "GB"];
        foreach (var unit in units)
        {
            if (size < 1024 || unit == "GB")
            {
                return $"{size:0.#} {unit}";
            }

            size /= 1024d;
        }

        return $"{bytes} B";
    }

    private sealed record ClientInstallerCandidate(FileInfo FileInfo, string VersionText, int[] VersionParts);
}
