using System;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using Yitter.IdGenerator;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 项目初始化服务
/// </summary>
public partial class ProjectInitializationService
{
    private readonly IConfiguration _configuration;
    private Action<string, string, string, int>? _progressCallback;

    public ProjectInitializationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 设置进度回调
    /// </summary>
    public void SetProgressCallback(Action<string, string, string, int>? callback)
    {
        _progressCallback = callback;
    }

    /// <summary>
    /// 发送进度消息
    /// </summary>
    private void SendProgress(string projectId, string step, string message, int progress)
    {
        _progressCallback?.Invoke(projectId, step, message, progress);
    }

    /// <summary>
    /// 初始化项目
    /// </summary>
    /// <param name="projectName">项目名称（如：OrderManager）</param>
    /// <param name="projectPath">项目输出路径</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="templateZipPath">模板zip文件路径</param>
    /// <param name="frontendPort">前端端口（可选）</param>
    /// <param name="backendPort">后端端口（可选）</param>
    /// <param name="projectId">项目ID（用于进度推送）</param>
    public async Task<bool> InitializeProjectAsync(
        string projectName,
        string projectPath,
        string databaseType,
        string connectionString,
        string templateZipPath,
        int frontendPort = 0,
        int backendPort = 0,
        string projectId = null,
        string displayName = null)
    {
        try
        {
            // 1. 验证模板文件存在
            SendProgress(projectId, "validate", "验证模板文件...", 5);
            if (!File.Exists(templateZipPath))
            {
                throw new FileNotFoundException($"Template file not found: {templateZipPath}");
            }

            // 2. 创建项目目录（如果不存在）
            SendProgress(projectId, "create_dir", "创建项目目录...", 10);
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }

            // 3. 解压模板（按 ZIP entry 逐文件补齐：缺失文件补全，已存在文件按合并策略处理）
            var state = await GetInitializationStateAsync(projectPath);
            SendProgress(projectId, "extract", "解压模板文件...", 20);
            await ExtractTemplateWithReplacementAsync(templateZipPath, projectPath, projectName, overwriteExisting: false);
            if (!state.Extracted)
            {
                state.Extracted = true;
                await SaveInitializationStateAsync(projectPath, state);
            }

            // 4. 执行核心初始化逻辑
            SendProgress(projectId, "initialize", "初始化项目配置...", 30);
            await InitializeProjectCoreAsync(projectPath, projectName, databaseType, connectionString, frontendPort, backendPort, projectId, displayName);
            await WriteProjectContextAsync(projectPath, projectName, projectId, displayName, databaseType, frontendPort, backendPort);

            SendProgress(projectId, "completed", "初始化完成", 100);
            return true;
        }
        catch (Exception ex)
        {
            SendProgress(projectId, "error", $"初始化失败: {ex.Message}", 0);
            throw new Exception($"Failed to initialize project: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 Base64 初始化项目（用于客户端）
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <param name="projectPath">项目输出路径</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="templateBase64">模板 ZIP 的 Base64 编码</param>
    /// <param name="frontendPort">前端端口（可选）</param>
    /// <param name="backendPort">后端端口（可选）</param>
    public async Task<bool> InitializeProjectFromBase64Async(
        string projectName,
        string projectPath,
        string databaseType,
        string connectionString,
        string templateBase64,
        int frontendPort = 0,
        int backendPort = 0)
    {
        try
        {
            // 1. 创建项目目录
            if (Directory.Exists(projectPath))
            {
                throw new InvalidOperationException($"Project directory already exists: {projectPath}");
            }
            Directory.CreateDirectory(projectPath);

            // 2. 将 Base64 解码为字节数组并解压
            var zipBytes = Convert.FromBase64String(templateBase64);
            await ExtractTemplateFromBytesAsync(zipBytes, projectPath);

            // 3. 执行核心初始化逻辑
            await InitializeProjectCoreAsync(projectPath, projectName, databaseType, connectionString, frontendPort, backendPort);
            await WriteProjectContextAsync(projectPath, projectName, null, null, databaseType, frontendPort, backendPort);

            return true;
        }
        catch (Exception ex)
        {
            // 如果失败，清理已创建的目录
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
            throw new Exception($"Failed to initialize project from base64: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 核心初始化逻辑（服务端和客户端共用）
    /// </summary>
    private async Task InitializeProjectCoreAsync(
        string projectPath,
        string projectName,
        string databaseType,
        string connectionString,
        int frontendPort = 0,
        int backendPort = 0,
        string projectId = null,
        string displayName = null)
    {
        // 获取初始化状态
        var state = await GetInitializationStateAsync(projectPath);

        // 1. 重命名文件和目录
        if (!state.Renamed)
        {
            SendProgress(projectId, "rename", "重命名文件和目录...", 40);
            await RenameFilesAndDirectoriesAsync(projectPath, projectName);
            state.Renamed = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 2. 替换项目名称和命名空间
        if (!state.Replaced)
        {
            SendProgress(projectId, "replace", "替换项目名称和命名空间...", 50);
            await ReplaceProjectNameAsync(projectPath, projectName);
            state.Replaced = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 3. 生成 .sln 文件
        await RemoveClientShellBridgeFromGeneratedMainAsync(projectPath, projectName);

        if (!state.SolutionGenerated)
        {
            SendProgress(projectId, "generate_sln", "生成解决方案文件...", 60);
            await GenerateSolutionFileAsync(projectPath, projectName);
            state.SolutionGenerated = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 4. 更新数据库配置
        if (!state.DatabaseConfigured)
        {
            SendProgress(projectId, "update_db", "更新数据库配置...", 65);
            await UpdateDatabaseConfigAsync(projectPath, projectName, databaseType, connectionString);
            state.DatabaseConfigured = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 5. 更新端口配置
        if (!state.PortConfigured && (frontendPort > 0 || backendPort > 0))
        {
            SendProgress(projectId, "update_port", "更新端口配置...", 70);
            await UpdatePortConfigAsync(projectPath, projectName, frontendPort, backendPort);
            state.PortConfigured = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 6. 运行数据库迁移
        if (!state.MigrationApplied)
        {
            SendProgress(projectId, "migration", "运行数据库迁移...", 75);
            await RunDatabaseMigrationAsync(projectPath, projectName, projectId);
            state.MigrationApplied = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 7. 运行 dotnet restore
        if (!state.DotnetRestored)
        {
            SendProgress(projectId, "dotnet_restore", "还原后端依赖...", 82);
            await RunDotnetRestoreAsync(projectPath, projectName);
            state.DotnetRestored = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 8. 写入项目翻译
        if (!state.TranslationsWritten && !string.IsNullOrEmpty(displayName))
        {
            SendProgress(projectId, "write_translations", "写入项目翻译...", 85);
            await WriteProjectTranslationsAsync(projectPath, projectName, displayName);
            state.TranslationsWritten = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 9. 运行 npm install
        if (!state.NpmInstalled)
        {
            SendProgress(projectId, "npm_install", "安装前端依赖...", 88);
            await RunNpmInstallAsync(projectPath, projectName);
            state.NpmInstalled = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 10. 启动后端服务
        if (!state.BackendStarted && backendPort > 0)
        {
            SendProgress(projectId, "start_backend", "启动后端服务...", 93);
            await StartBackendAsync(projectPath, projectName, backendPort);
            state.BackendStarted = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 11. 启动前端服务
        if (!state.FrontendStarted && frontendPort > 0)
        {
            SendProgress(projectId, "start_frontend", "启动前端服务...", 96);
            await StartFrontendAsync(projectPath, projectName, frontendPort);
            state.FrontendStarted = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 解压模板
    /// </summary>
    private async Task ExtractTemplateAsync(string zipPath, string destPath)
    {
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(zipPath, destPath);
        });

        // 验证解压是否成功
        var slnFile = Path.Combine(destPath, "CodeMaster.sln");
        if (!File.Exists(slnFile))
        {
            throw new Exception($"Template extraction failed: CodeMaster.sln not found in {destPath}");
        }
    }

    /// <summary>
    /// 从字节数组解压模板（用于客户端）
    /// </summary>
    private async Task ExtractTemplateFromBytesAsync(byte[] zipBytes, string destPath)
    {
        await Task.Run(() =>
        {
            using var memoryStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(destPath);
        });

        // 验证解压是否成功
        var slnFile = Path.Combine(destPath, "CodeMaster.sln");
        if (!File.Exists(slnFile))
        {
            throw new Exception($"Template extraction failed: CodeMaster.sln not found in {destPath}");
        }
    }

    /// <summary>
    /// 替换项目名称和命名空间
    /// </summary>
    private async Task ReplaceProjectNameAsync(string projectPath, string newProjectName)
    {
        // 注意：.sln 文件由 GenerateSolutionFileAsync 单独处理
        var fileExtensions = new[] { ".cs", ".csproj", ".json", ".js", ".vue", ".ts", ".tsx", ".html" };

        var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => fileExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        foreach (var file in files)
        {
            await ReplaceInFileAsync(file, "CodeMaster", newProjectName);
        }
    }

    private async Task RemoveClientShellBridgeFromGeneratedMainAsync(string projectPath, string projectName)
    {
        var mainPath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "main.js");
        var utf8WithoutBom = new UTF8Encoding(false);

        if (File.Exists(mainPath))
        {
            var lines = await File.ReadAllLinesAsync(mainPath, utf8WithoutBom);
            var result = lines
                .Where(line =>
                    !line.Contains("tauriBridge", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("installTauriBridge", StringComparison.OrdinalIgnoreCase))
                .ToList();

            await File.WriteAllLinesAsync(mainPath, result, utf8WithoutBom);
        }

        var requestPath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "utils", "request.js");
        if (File.Exists(requestPath))
        {
            await File.WriteAllTextAsync(requestPath, GetCleanGeneratedRequestJs(), utf8WithoutBom);
        }

        var loginPath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "views", "login", "index.vue");
        if (File.Exists(loginPath))
        {
            var content = await File.ReadAllTextAsync(loginPath, utf8WithoutBom);
            content = GeneratedTemplateCleanup.RemoveLoginClientBridge(content);
            await File.WriteAllTextAsync(loginPath, content, utf8WithoutBom);
        }
    }

    private static string GetCleanGeneratedRequestJs()
    {
        return """
import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import i18n from '@/i18n'

const t = (key, params) => i18n.global.t(key, params)

const service = axios.create({
  baseURL: '/api',
  timeout: 10000
})

service.interceptors.request.use(
  config => {
    const userStore = localStorage.getItem('user')
    if (userStore) {
      try {
        const user = JSON.parse(userStore)
        if (user.token) {
          config.headers['Authorization'] = `Bearer ${user.token}`
        }
      } catch (e) {
        console.error('Failed to parse user store:', e)
      }
    }

    const settingsStore = localStorage.getItem('settings')
    if (settingsStore) {
      try {
        const settings = JSON.parse(settingsStore)
        if (settings.language) {
          config.headers['Accept-Language'] = settings.language
        }
      } catch (e) {
        console.error('Failed to parse settings store:', e)
      }
    }

    return config
  },
  error => {
    console.error('Request error:', error)
    return Promise.reject(error)
  }
)

service.interceptors.response.use(
  response => {
    const res = response.data

    if (res && typeof res === 'object' && !Array.isArray(res) && typeof res.code === 'number') {
      if (res.code === 200) {
        return res.data
      }

      ElMessage.error(res.message || t('request_failed'))
      return Promise.reject(new Error(res.message || t('request_failed')))
    }

    return res
  },
  error => {
    console.error('Response error:', error)

    if (error.response && error.response.status === 401) {
      ElMessage.error(t('login_expired'))
      localStorage.removeItem('user')
      localStorage.removeItem('permission')
      router.push('/login')
    } else {
      const errorMsg = error.response?.data?.message || error.message
      if (errorMsg) {
        ElMessage.error(errorMsg)
      }
    }

    return Promise.reject(error)
  }
)

export default service
""";
    }

    /// <summary>
    /// 替换文件内容
    /// </summary>
    private async Task ReplaceInFileAsync(string filePath, string oldValue, string newValue)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, new UTF8Encoding(false));

            if (content.Contains(oldValue))
            {
                content = content.Replace(oldValue, newValue);
                await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(false));
            }
        }
        catch (Exception ex)
        {
            // 忽略二进制文件或无法读取的文件
            Console.WriteLine($"Skip file {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新数据库配置
    /// </summary>
    private async Task UpdateDatabaseConfigAsync(string projectPath, string projectName, string databaseType, string connectionString)
    {
        // 对于 SQLite，使用绝对路径
        var finalConnectionString = connectionString;
        if (databaseType == "SQLite" && connectionString.StartsWith("Data Source="))
        {
            var dbFileName = connectionString.Replace("Data Source=", "").Trim().Trim('"');
            if (!Path.IsPathRooted(dbFileName))
            {
                var absolutePath = Path.GetFullPath(Path.Combine(projectPath, $"{projectName}.Migrator", dbFileName));
                finalConnectionString = $"Data Source={absolutePath}";
            }
            else
            {
                finalConnectionString = $"Data Source={dbFileName}";
            }
        }

        var utf8WithoutBom = new UTF8Encoding(false);

        // 计算 DbProvider（数据库类型）
        var dbProvider = databaseType switch
        {
            "MySQL" => "MySql",
            "SqlServer" => "SqlServer",
            "PostgreSQL" => "PostgreSQL",
            "SQLite" => "Sqlite",
            "Oracle" => "Oracle",
            _ => "SqlServer"
        };

        // 更新WebApi的appsettings.json
        var webApiAppsettingsJsonPath = Path.Combine(projectPath, $"{projectName}.WebApi", "appsettings.json");
        if (File.Exists(webApiAppsettingsJsonPath))
        {
            var content = await File.ReadAllTextAsync(webApiAppsettingsJsonPath, utf8WithoutBom);
            content = Regex.Replace(content,
                @"""DefaultConnection"":\s*""[^""]*""",
                $"\"DefaultConnection\": \"{finalConnectionString.Replace("\\", "\\\\")}\"");
            content = Regex.Replace(content,
                @"""DbProvider"":\s*""[^""]*""",
                $"\"DbProvider\": \"{dbProvider}\"");
            await File.WriteAllTextAsync(webApiAppsettingsJsonPath, content, utf8WithoutBom);
        }

        // 更新WebApi的appsettings.Development.json
        var webApiAppsettingsPath = Path.Combine(projectPath, $"{projectName}.WebApi", "appsettings.Development.json");
        if (File.Exists(webApiAppsettingsPath))
        {
            var content = await File.ReadAllTextAsync(webApiAppsettingsPath, utf8WithoutBom);
            content = Regex.Replace(content,
                @"""DefaultConnection"":\s*""[^""]*""",
                $"\"DefaultConnection\": \"{finalConnectionString.Replace("\\", "\\\\")}\"");
            content = Regex.Replace(content,
                @"""DbProvider"":\s*""[^""]*""",
                $"\"DbProvider\": \"{dbProvider}\"");
            await File.WriteAllTextAsync(webApiAppsettingsPath, content, utf8WithoutBom);
        }

        // 更新Migrator的appsettings.json
        var migratorAppsettingsPath = Path.Combine(projectPath, $"{projectName}.Migrator", "appsettings.json");
        if (File.Exists(migratorAppsettingsPath))
        {
            var content = await File.ReadAllTextAsync(migratorAppsettingsPath, utf8WithoutBom);

            // 替换DefaultConnection连接字符串（SQLite 使用绝对路径）
            content = Regex.Replace(content,
                @"""DefaultConnection"":\s*""[^""]*""",
                $"\"DefaultConnection\": \"{finalConnectionString.Replace("\\", "\\\\")}\"");

            // 替换DbProvider（数据库类型）
            content = Regex.Replace(content,
                @"""DbProvider"":\s*""[^""]*""",
                $"\"DbProvider\": \"{dbProvider}\"");

            await File.WriteAllTextAsync(migratorAppsettingsPath, content, utf8WithoutBom);
        }

        // 更新 Migrator.csproj 默认 DbProvider，初始化后无需额外传 /p:DbProvider
        var migratorCsprojPath = Path.Combine(projectPath, $"{projectName}.Migrator", $"{projectName}.Migrator.csproj");
        if (File.Exists(migratorCsprojPath))
        {
            var csprojContent = await File.ReadAllTextAsync(migratorCsprojPath, utf8WithoutBom);

            // 将带 Condition 的默认值改写为固定值
            csprojContent = Regex.Replace(
                csprojContent,
                @"<DbProvider\s+Condition=\""'\$\(DbProvider\)'==''\"">[^<]*</DbProvider>",
                $"<DbProvider>{dbProvider}</DbProvider>");

            await File.WriteAllTextAsync(migratorCsprojPath, csprojContent, utf8WithoutBom);
        }
    }

    /// <summary>
    /// 更新端口配置
    /// </summary>
    private async Task UpdatePortConfigAsync(string projectPath, string projectName, int frontendPort, int backendPort)
    {
        var utf8WithoutBom = new UTF8Encoding(false);

        // 1. 更新后端端口 (launchSettings.json)
        if (backendPort > 0)
        {
            var launchSettingsPath = Path.Combine(projectPath, $"{projectName}.WebApi", "Properties", "launchSettings.json");
            if (File.Exists(launchSettingsPath))
            {
                var content = await File.ReadAllTextAsync(launchSettingsPath, utf8WithoutBom);

                // 替换 http 配置中的端口
                content = Regex.Replace(content,
                    @"""applicationUrl"":\s*""http://localhost:\d+""",
                    $"\"applicationUrl\": \"http://localhost:{backendPort}\"");

                // 替换 https 配置中的端口
                content = Regex.Replace(content,
                    @"""applicationUrl"":\s*""https://localhost:\d+;http://localhost:\d+""",
                    $"\"applicationUrl\": \"https://localhost:{backendPort + 1};http://localhost:{backendPort}\"");

                await File.WriteAllTextAsync(launchSettingsPath, content, utf8WithoutBom);
            }
        }

        // 2. 更新前端端口和代理配置 (vite.config.js)
        if (frontendPort > 0 || backendPort > 0)
        {
            var viteConfigPath = Path.Combine(projectPath, $"{projectName}.Vue", "vite.config.js");
            if (File.Exists(viteConfigPath))
            {
                var content = await File.ReadAllTextAsync(viteConfigPath, utf8WithoutBom);

                // 更新前端端口
                if (frontendPort > 0)
                {
                    content = Regex.Replace(content,
                        @"port:\s*\d+",
                        $"port: {frontendPort}");
                }

                // 更新代理目标端口
                if (backendPort > 0)
                {
                    content = Regex.Replace(content,
                        @"target:\s*'http://localhost:\d+'",
                        $"target: 'http://localhost:{backendPort}'");
                }

                await File.WriteAllTextAsync(viteConfigPath, content, utf8WithoutBom);
            }
        }

        // 3. 更新前端项目配置中的端口 (edit.vue 中的默认值)
        var editVuePath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "views", "codegen", "project", "edit.vue");
        if (File.Exists(editVuePath))
        {
            var content = await File.ReadAllTextAsync(editVuePath, utf8WithoutBom);

            if (frontendPort > 0)
            {
                content = Regex.Replace(content,
                    @"frontendPort:\s*\d+",
                    $"frontendPort: {frontendPort}");
            }

            if (backendPort > 0)
            {
                content = Regex.Replace(content,
                    @"backendPort:\s*\d+",
                    $"backendPort: {backendPort}");
            }

            await File.WriteAllTextAsync(editVuePath, content, utf8WithoutBom);
        }
    }

    /// <summary>
    /// 重命名文件和目录
    /// </summary>
    private async Task RenameFilesAndDirectoriesAsync(string projectPath, string newProjectName)
    {
        await Task.Run(() =>
        {
            Console.WriteLine($"[Rename] Starting rename in: {projectPath}");

            // 重命名目录（从最深层开始，确保子目录先被重命名）
            var directories = Directory.GetDirectories(projectPath, "*CodeMaster*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();

            Console.WriteLine($"[Rename] Found {directories.Count} directories to rename");

            foreach (var dir in directories)
            {
                var parentDir = Path.GetDirectoryName(dir);
                var dirName = Path.GetFileName(dir);
                var newDir = string.IsNullOrEmpty(parentDir)
                    ? dirName.Replace("CodeMaster", newProjectName)
                    : Path.Combine(parentDir, dirName.Replace("CodeMaster", newProjectName));
                if (dir != newDir && !Directory.Exists(newDir))
                {
                    Console.WriteLine($"[Rename] Renaming directory: {dir} -> {newDir}");
                    Directory.Move(dir, newDir);
                }
                else if (dir != newDir && Directory.Exists(newDir))
                {
                    Console.WriteLine($"[Rename] Skip directory (already exists): {newDir}");
                }
            }

            // 重命名文件
            var files = Directory.GetFiles(projectPath, "*CodeMaster*", SearchOption.AllDirectories)
                .ToList();

            Console.WriteLine($"[Rename] Found {files.Count} files to rename");

            foreach (var file in files)
            {
                var parentDir = Path.GetDirectoryName(file);
                var fileName = Path.GetFileName(file);
                var newFile = string.IsNullOrEmpty(parentDir)
                    ? fileName.Replace("CodeMaster", newProjectName)
                    : Path.Combine(parentDir, fileName.Replace("CodeMaster", newProjectName));
                if (file != newFile && !File.Exists(newFile))
                {
                    Console.WriteLine($"[Rename] Renaming file: {file} -> {newFile}");
                    File.Move(file, newFile);
                }
                else if (file != newFile && File.Exists(newFile))
                {
                    Console.WriteLine($"[Rename] Skip file (already exists): {newFile}");
                }
            }

            Console.WriteLine($"[Rename] Rename completed");
        });
    }

    /// <summary>
    /// 生成解决方案文件
    /// </summary>
    private async Task GenerateSolutionFileAsync(string projectPath, string newProjectName)
    {
        // 规范化路径，确保使用正确的分隔符
        projectPath = Path.GetFullPath(projectPath);

        // 注意：.sln 文件已经被 RenameFilesAndDirectoriesAsync 重命名了
        var slnPath = Path.Combine(projectPath, $"{newProjectName}.sln");

        if (!File.Exists(slnPath))
        {
            throw new FileNotFoundException($"Solution file not found: {slnPath}");
        }

        // 读取并替换内容中的 CodeMaster 为新项目名
        var content = await File.ReadAllTextAsync(slnPath, new UTF8Encoding(false));
        content = content.Replace("CodeMaster", newProjectName);

        // 移除不需要的项目引用（CodeGenerator 和测试项目）
        var projectsToRemove = new[]
        {
            $"{newProjectName}.CodeGenerator",
            $"{newProjectName}.CodeGenerator.Tests",
            $"{newProjectName}.OpenSpec.Tests",
            $"{newProjectName}.E2eTests",
            $"{newProjectName}.McpServer",
            $"{newProjectName}.LocalAgent",
            $"{newProjectName}.LocalAgent.Tests"
        };

        content = RemoveSolutionProjects(content, projectsToRemove);
        await File.WriteAllTextAsync(slnPath, content, new UTF8Encoding(false));
    }

    private static string RemoveSolutionProjects(string content, IReadOnlyCollection<string> projectNames)
    {
        var projectNameSet = new HashSet<string>(projectNames, StringComparer.OrdinalIgnoreCase);
        var projectGuidsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var keptLines = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = Regex.Match(line,
                "^Project\\(\"\\{[^}]+\\}\"\\) = \"(?<name>[^\"]+)\", \"[^\"]+\", \"(?<guid>\\{[^}]+\\})\"");

            if (match.Success && projectNameSet.Contains(match.Groups["name"].Value))
            {
                projectGuidsToRemove.Add(match.Groups["guid"].Value);
                while (i + 1 < lines.Length &&
                       !lines[i + 1].Trim().Equals("EndProject", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                }

                if (i + 1 < lines.Length)
                {
                    i++;
                }

                continue;
            }

            keptLines.Add(line);
        }

        if (projectGuidsToRemove.Count == 0)
        {
            return string.Join(Environment.NewLine, keptLines);
        }

        return string.Join(Environment.NewLine,
            keptLines.Where(line => !projectGuidsToRemove.Any(guid =>
                line.Contains(guid, StringComparison.OrdinalIgnoreCase))));
    }

    /// <summary>
    /// 运行数据库迁移
    /// </summary>
    public async Task<bool> RunDatabaseMigrationAsync(string projectPath, string projectName, string projectId = null)
    {
        var migratorPath = Path.Combine(projectPath, $"{projectName}.Migrator");

        Console.WriteLine($"[Migration] Migrator Path: {migratorPath}");

        if (!Directory.Exists(migratorPath))
        {
            throw new DirectoryNotFoundException($"Migrator project not found: {migratorPath}");
        }

        try
        {
            // 检查 appsettings.json 是否存在
            var appsettingsPath = Path.Combine(migratorPath, "appsettings.json");
            if (!File.Exists(appsettingsPath))
            {
                throw new FileNotFoundException($"appsettings.json not found in Migrator project: {appsettingsPath}");
            }

            // 读取并验证数据库配置
            var appsettingsContent = await File.ReadAllTextAsync(appsettingsPath);
            Console.WriteLine($"[Migration] appsettings.json content:\n{appsettingsContent}");

            // 检查是否已存在 InitialCreate 迁移文件
            var migrationsPath = Path.Combine(migratorPath, "Migrations");

            // 如果 Migrations 目录存在且包含迁移文件，先删除所有迁移文件
            if (Directory.Exists(migrationsPath))
            {
                var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs");
                if (migrationFiles.Length > 0)
                {
                    Console.WriteLine($"[Migration] Found {migrationFiles.Length} existing migration files, deleting them...");
                    foreach (var file in migrationFiles)
                    {
                        Console.WriteLine($"[Migration] Deleting: {file}");
                        File.Delete(file);
                    }
                }
            }

            Console.WriteLine($"[Migration] No existing migrations, will create new InitialCreate migration");

            // 1. 创建 InitialCreate 迁移
            SendProgress(projectId, "migration_add", "创建数据库迁移...", 75);
            Console.WriteLine("[Migration] Creating InitialCreate migration...");

            try
            {
                // 先构建，使用初始化阶段写入到 csproj 的 DbProvider
                await RunProcessWithTimeoutAsync(
                    "dotnet",
                    "build",
                    migratorPath,
                    TimeSpan.FromMinutes(3));

                // 再创建迁移，避免 ef 将 -p 识别为 --project 参数
                await RunProcessWithTimeoutAsync(
                    "dotnet",
                    "ef migrations add InitialCreate --no-build",
                    migratorPath,
                    TimeSpan.FromMinutes(2));
                Console.WriteLine("[Migration] InitialCreate migration created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Failed to create migration: {ex.Message}");
                throw new Exception($"Failed to create database migration. Please check:\n" +
                    $"1. EF Core tools are installed (dotnet tool install --global dotnet-ef)\n" +
                    $"2. Database connection string is correct\n" +
                    $"3. Database server is running\n\n" +
                    $"Error: {ex.Message}", ex);
            }

            // 2. 执行 dotnet run 来应用迁移
            SendProgress(projectId, "migration_run", "应用数据库迁移...", 80);
            Console.WriteLine("[Migration] Applying database migration (dotnet run)...");

            try
            {
                await RunProcessWithTimeoutAsync(
                    "dotnet",
                    "run",
                    migratorPath,
                    TimeSpan.FromMinutes(5));
                Console.WriteLine("[Migration] Database migration applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Failed to apply migration: {ex.Message}");
                throw new Exception($"Failed to apply database migration. Please check:\n" +
                    $"1. Database connection string is correct\n" +
                    $"2. Database server is running and accessible\n" +
                    $"3. Database user has sufficient permissions\n" +
                    $"4. No syntax errors in migration files\n\n" +
                    $"Error: {ex.Message}", ex);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Migration] Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 运行 npm install（安装前端依赖）
    /// </summary>
    /// <summary>
    /// 应用已创建的数据库迁移。分步初始化的 Step5 已经创建迁移，Step6 只负责运行 Migrator。
    /// </summary>
    private async Task ApplyExistingDatabaseMigrationAsync(string projectPath, string projectName, string projectId = null)
    {
        var migratorPath = Path.Combine(projectPath, $"{projectName}.Migrator");
        if (!Directory.Exists(migratorPath))
        {
            throw new DirectoryNotFoundException($"Migrator project not found: {migratorPath}");
        }

        SendProgress(projectId, "migration_run", "应用数据库迁移...", 80);
        Console.WriteLine("[Migration] Applying existing database migration (dotnet run)...");

        try
        {
            await RunProcessWithTimeoutAsync(
                "dotnet",
                "run",
                migratorPath,
                TimeSpan.FromMinutes(5));
            Console.WriteLine("[Migration] Existing database migration applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Migration] Failed to apply existing migration: {ex.Message}");
            throw new Exception($"Failed to apply database migration. Please check:\n" +
                $"1. Step 5 has created the migration files\n" +
                $"2. Database connection string is correct\n" +
                $"3. Database server is running and accessible\n" +
                $"4. Database user has sufficient permissions\n\n" +
                $"Error: {ex.Message}", ex);
        }
    }

    public async Task<bool> RunNpmInstallAsync(string projectPath, string projectName, string projectId = null)
    {
        var vuePath = Path.Combine(projectPath, $"{projectName}.Vue");

        if (!Directory.Exists(vuePath))
        {
            throw new DirectoryNotFoundException($"Vue project not found: {vuePath}");
        }

        try
        {
            SendProgress(projectId, "npm_install", "安装前端依赖...", 90);
            await RunProcessWithTimeoutAsync(
                "cmd.exe",
                "/c npm install --legacy-peer-deps",
                vuePath,
                TimeSpan.FromMinutes(5));

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to run npm install: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 Migrator appsettings.json 读取 DbProvider，并标准化为 MSBuild 参数值
    /// </summary>
    private async Task<string> GetMigratorDbProviderAsync(string migratorPath)
    {
        var appsettingsPath = Path.Combine(migratorPath, "appsettings.json");
        if (!File.Exists(appsettingsPath))
        {
            return "SqlServer";
        }

        var json = await File.ReadAllTextAsync(appsettingsPath);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("DbProvider", out var providerElement))
        {
            return "SqlServer";
        }

        var provider = providerElement.GetString() ?? "SqlServer";
        return provider switch
        {
            "MySQL" => "MySql",
            "PostgreSql" => "PostgreSQL",
            _ => provider
        };
    }

    /// <summary>
    /// 运行进程并设置超时
    /// </summary>
    private async Task RunProcessWithTimeoutAsync(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Console.WriteLine($"[Process] Starting: {fileName} {arguments}");
        Console.WriteLine($"[Process] Working Directory: {workingDirectory}");

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new Exception($"Failed to start process: {fileName} {arguments}");
        }

        // 异步读取输出和错误流
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Process Output] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Process Error] {e.Data}");
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cts.Token);

            // 等待异步读取完成
            await Task.Delay(100);

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            Console.WriteLine($"[Process] Exit Code: {process.ExitCode}");

            // 检查退出码
            if (process.ExitCode != 0)
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Process exited with code {process.ExitCode}");
                errorMessage.AppendLine($"Command: {fileName} {arguments}");
                errorMessage.AppendLine($"Working Directory: {workingDirectory}");

                if (!string.IsNullOrEmpty(error))
                {
                    errorMessage.AppendLine($"Error Output:");
                    errorMessage.AppendLine(error);
                }

                if (!string.IsNullOrEmpty(output))
                {
                    errorMessage.AppendLine($"Standard Output:");
                    errorMessage.AppendLine(output);
                }

                throw new Exception(errorMessage.ToString());
            }
        }
        catch (OperationCanceledException)
        {
            // 超时，强制终止进程
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { }

            throw new TimeoutException($"Process timed out after {timeout.TotalMinutes} minutes: {fileName} {arguments}");
        }
    }

    #region 断点续传机制

    /// <summary>
    /// 初始化状态
    /// </summary>
    public class InitializationState
    {
        public bool Extracted { get; set; }
        public bool Renamed { get; set; }
        public bool Replaced { get; set; }
        public bool SolutionGenerated { get; set; }
        public bool DatabaseConfigured { get; set; }
        public bool PortConfigured { get; set; }
        public bool MigrationCreated { get; set; }
        public bool MigrationApplied { get; set; }
        public bool DotnetRestored { get; set; }
        public bool TranslationsWritten { get; set; }
        public bool NpmInstalled { get; set; }
        public bool BackendStarted { get; set; }
        public bool FrontendStarted { get; set; }
    }

    public class ProjectContext
    {
        public int SchemaVersion { get; set; } = 1;
        public string CreatedBy { get; set; } = "CodeMaster";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ServerBaseUrl { get; set; }
        public string? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? DatabaseType { get; set; }
        public int FrontendPort { get; set; }
        public int BackendPort { get; set; }
    }

    /// <summary>
    /// 获取初始化状态
    /// </summary>
    /// <summary>
    /// 获取初始化状态
    /// </summary>
    public async Task<InitializationState> GetInitializationStateAsync(string projectPath)
    {
        var stateFile = Path.Combine(projectPath, ".codemaster-init-state.json");

        if (!File.Exists(stateFile))
        {
            return new InitializationState();
        }

        try
        {
            var json = await File.ReadAllTextAsync(stateFile);
            return JsonSerializer.Deserialize<InitializationState>(json) ?? new InitializationState();
        }
        catch
        {
            return new InitializationState();
        }
    }

    /// <summary>
    /// 保存初始化状态
    /// </summary>
    private async Task SaveInitializationStateAsync(string projectPath, InitializationState state)
    {
        var stateFile = Path.Combine(projectPath, ".codemaster-init-state.json");
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(stateFile, json, new UTF8Encoding(false));
    }

    public async Task WriteProjectContextAsync(
        string projectPath,
        string projectName,
        string? projectId,
        string? displayName,
        string? databaseType,
        int frontendPort,
        int backendPort,
        string? serverBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return;

        var contextDir = Path.Combine(projectPath, ".codemaster");
        Directory.CreateDirectory(contextDir);

        var context = new ProjectContext
        {
            ServerBaseUrl = string.IsNullOrWhiteSpace(serverBaseUrl) ? null : serverBaseUrl.Trim().TrimEnd('/'),
            ProjectId = projectId,
            ProjectName = projectName,
            DisplayName = displayName,
            DatabaseType = databaseType,
            FrontendPort = frontendPort,
            BackendPort = backendPort
        };

        var json = JsonSerializer.Serialize(context, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(Path.Combine(contextDir, "project-context.json"), json, new UTF8Encoding(false));
    }

    #endregion

    #region 智能 ZIP 解压

    /// <summary>
    /// 智能解压模板（在解压时替换项目名称，支持文件合并）
    /// </summary>
    private async Task ExtractTemplateWithReplacementAsync(
        string zipPath,
        string destPath,
        string projectName,
        bool overwriteExisting = false)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            // 跳过目录条目
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            // 1. 替换路径中的项目名称
            var relativePath = entry.FullName.Replace("CodeMaster", projectName);
            var destFile = Path.Combine(destPath, relativePath);

            // 2. 创建目录
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // 3. 判断文件是否已存在
            if (File.Exists(destFile) && !overwriteExisting)
            {
                // 智能合并逻辑
                await MergeFileAsync(entry, destFile, projectName);
            }
            else
            {
                // 解压并替换内容
                await ExtractAndReplaceAsync(entry, destFile, projectName);
            }
        }
    }

    /// <summary>
    /// 解压文件并替换项目名称
    /// </summary>
    private async Task ExtractAndReplaceAsync(ZipArchiveEntry entry, string destFile, string projectName)
    {
        // 读取文件内容
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        var content = await reader.ReadToEndAsync();

        // 替换项目名称
        content = content.Replace("CodeMaster", projectName);

        // 写入文件（使用 UTF-8 without BOM）
        await File.WriteAllTextAsync(destFile, content, new UTF8Encoding(false));
    }

    #endregion

    #region 文件合并

    /// <summary>
    /// 合并文件（仅配置文件，代码文件跳过）
    /// </summary>
    private async Task MergeFileAsync(ZipArchiveEntry entry, string destFile, string projectName)
    {
        var fileName = Path.GetFileName(destFile);

        // 仅合并配置文件，代码文件完全跳过
        if (fileName == "vite.config.js")
        {
            await MergeViteConfigAsync(entry, destFile);
        }
        else if (fileName == "appsettings.json" || fileName == "appsettings.Development.json")
        {
            await MergeJsonConfigAsync(entry, destFile, projectName);
        }
        else if (fileName == "launchSettings.json")
        {
            await MergeLaunchSettingsAsync(entry, destFile);
        }
        else
        {
            // 所有其他文件（包括 .cs, .vue 等代码文件）：完全跳过，保留用户修改
            Console.WriteLine($"[Merge] Skip existing file (preserve user changes): {destFile}");
        }
    }

    /// <summary>
    /// 合并 JSON 配置文件
    /// </summary>
    private async Task MergeJsonConfigAsync(ZipArchiveEntry entry, string destFile, string projectName)
    {
        try
        {
            var utf8WithoutBom = new UTF8Encoding(false);

            // 读取模板内容
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, utf8WithoutBom);
            var templateContent = await reader.ReadToEndAsync();
            templateContent = templateContent.Replace("CodeMaster", projectName);
            var templateJson = JsonDocument.Parse(templateContent);

            // 读取现有内容
            var existingContent = await File.ReadAllTextAsync(destFile, utf8WithoutBom);
            var existingJson = JsonDocument.Parse(existingContent);

            // 深度合并 JSON
            var merged = MergeJsonObjects(existingJson.RootElement, templateJson.RootElement);

            // 写回文件（使用 UTF-8 without BOM）
            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(destFile, JsonSerializer.Serialize(merged, options), utf8WithoutBom);

            Console.WriteLine($"[Merge] Merged JSON config: {destFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Merge] Failed to merge JSON {destFile}: {ex.Message}");
        }
    }

    /// <summary>
    /// 深度合并 JSON 对象
    /// </summary>
    private Dictionary<string, object> MergeJsonObjects(JsonElement existing, JsonElement template)
    {
        var result = new Dictionary<string, object>();

        // 先复制现有的所有属性
        foreach (var prop in existing.EnumerateObject())
        {
            result[prop.Name] = JsonElementToObject(prop.Value);
        }

        // 然后合并模板的属性
        foreach (var prop in template.EnumerateObject())
        {
            if (result.ContainsKey(prop.Name))
            {
                // 如果都是对象，递归合并
                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    result[prop.Name] is Dictionary<string, object>)
                {
                    var existingObj = existing.GetProperty(prop.Name);
                    result[prop.Name] = MergeJsonObjects(existingObj, prop.Value);
                }
                // 否则保留现有值（不覆盖用户配置）
            }
            else
            {
                // 新属性，添加
                result[prop.Name] = JsonElementToObject(prop.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// 将 JsonElement 转换为对象
    /// </summary>
    private object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(JsonElementToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    /// <summary>
    /// 合并 Vite 配置文件
    /// </summary>
    private async Task MergeViteConfigAsync(ZipArchiveEntry entry, string destFile)
    {
        try
        {
            // 读取现有配置
            var existingContent = await File.ReadAllTextAsync(destFile);

            // 使用正则表达式更新端口和代理配置
            // 这里简化处理，仅更新关键配置项
            Console.WriteLine($"[Merge] Vite config exists, preserving user changes: {destFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Merge] Failed to merge vite.config.js {destFile}: {ex.Message}");
        }
    }

    /// <summary>
    /// 合并 LaunchSettings 配置文件
    /// </summary>
    private async Task MergeLaunchSettingsAsync(ZipArchiveEntry entry, string destFile)
    {
        try
        {
            // 读取现有配置
            var existingContent = await File.ReadAllTextAsync(destFile);

            Console.WriteLine($"[Merge] LaunchSettings exists, preserving user changes: {destFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Merge] Failed to merge launchSettings.json {destFile}: {ex.Message}");
        }
    }

    #endregion

    #region 新增初始化步骤

    /// <summary>
    /// 运行 dotnet restore
    /// </summary>
    private async Task RunDotnetRestoreAsync(string projectPath, string projectName)
    {
        var slnPath = Path.Combine(projectPath, $"{projectName}.sln");

        if (!File.Exists(slnPath))
        {
            throw new FileNotFoundException($"Solution file not found: {slnPath}");
        }

        await RunProcessWithTimeoutAsync("dotnet", "restore", projectPath, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 写入项目翻译（使用 SqlSugar 直接写入数据库）
    /// </summary>
    private async Task WriteProjectTranslationsAsync(string projectPath, string projectName, string displayName)
    {
        try
        {
            // 1. 读取项目的数据库配置
            var appsettingsPath = Path.Combine(projectPath, $"{projectName}.WebApi", "appsettings.json");
            if (!File.Exists(appsettingsPath))
            {
                Console.WriteLine($"[Translation] appsettings.json not found: {appsettingsPath}");
                return;
            }

            var json = await File.ReadAllTextAsync(appsettingsPath);
            var config = JsonDocument.Parse(json);

            var connectionString = config.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString();
            var dbProvider = config.RootElement
                .GetProperty("DbProvider")
                .GetString();

            // 2. 使用 SqlSugar 连接数据库
            var dbType = dbProvider switch
            {
                "SqlServer" => SqlSugar.DbType.SqlServer,
                "MySql" => SqlSugar.DbType.MySql,
                "PostgreSql" => SqlSugar.DbType.PostgreSQL,
                "Sqlite" => SqlSugar.DbType.Sqlite,
                "Oracle" => SqlSugar.DbType.Oracle,
                _ => SqlSugar.DbType.SqlServer
            };

            var db = new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(dbType)
            });

            // 3. 检查翻译是否已存在
            var langKey = "app_title";
            var exists = await db.Queryable<SysLangText>()
                .Where(x => x.LangKey == langKey && x.LangCode == "zh-CN")
                .AnyAsync();

            if (!exists)
            {
                // 4. 插入翻译记录
                var langText = new SysLangText
                {
                    Id = YitIdHelper.NextId(),
                    LangKey = langKey,
                    LangCode = "zh-CN",
                    LangValue = displayName,
                    CreateTime = DateTime.UtcNow
                };

                await db.Insertable(langText).ExecuteCommandAsync();

                Console.WriteLine($"[Translation] Inserted translation: {langKey} = {displayName}");
            }
            else
            {
                // 更新已存在的翻译
                await db.Updateable<SysLangText>()
                    .SetColumns(x => x.LangValue == displayName)
                    .SetColumns(x => x.UpdateTime == DateTime.UtcNow)
                    .Where(x => x.LangKey == langKey && x.LangCode == "zh-CN")
                    .ExecuteCommandAsync();

                Console.WriteLine($"[Translation] Updated translation: {langKey} = {displayName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Translation] Failed to write translation: {ex.Message}");
            // 不抛出异常，翻译写入失败不应阻止初始化流程
        }
    }

    /// <summary>
    /// 启动后端服务（打开 CMD 窗口）
    /// </summary>
    private async Task StartBackendAsync(string projectPath, string projectName, int port)
    {
        var webApiPath = Path.Combine(projectPath, $"{projectName}.WebApi");

        if (!Directory.Exists(webApiPath))
        {
            throw new DirectoryNotFoundException($"WebApi directory not found: {webApiPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k dotnet run",  // /k 保持窗口打开
            WorkingDirectory = webApiPath,
            UseShellExecute = true,  // 显示窗口
            CreateNoWindow = false
        };

        Process.Start(startInfo);

        // 等待服务启动
        await WaitForServiceAsync($"http://localhost:{port}", timeout: 30000);
    }

    /// <summary>
    /// 启动前端服务（打开 CMD 窗口）
    /// </summary>
    private async Task StartFrontendAsync(string projectPath, string projectName, int port)
    {
        var vuePath = Path.Combine(projectPath, $"{projectName}.Vue");

        if (!Directory.Exists(vuePath))
        {
            throw new DirectoryNotFoundException($"Vue directory not found: {vuePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k npm run dev",  // /k 保持窗口打开
            WorkingDirectory = vuePath,
            UseShellExecute = true,  // 显示窗口
            CreateNoWindow = false
        };

        Process.Start(startInfo);

        // 等待服务启动
        await WaitForServiceAsync($"http://localhost:{port}", timeout: 60000);
    }

    /// <summary>
    /// 等待服务启动
    /// </summary>
    private async Task WaitForServiceAsync(string url, int timeout)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        var startTime = DateTime.UtcNow;

        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"[Service] Service started successfully: {url}");
                    return;
                }
            }
            catch
            {
                // 服务未启动，继续等待
            }

            await Task.Delay(2000);
        }

        Console.WriteLine($"[Service] Warning: Service at {url} did not respond within {timeout}ms, but continuing...");
        // 不抛出异常，服务启动失败不应阻止初始化流程
    }

    #endregion

    #region 分步初始化公共方法

    /// <summary>
    /// 步骤1：解压模板并替换项目名称
    /// </summary>
    public async Task Step1_ExtractTemplateAsync(string projectName, string projectPath, string templateZipPath, string projectId = null)
    {
        // 验证模板文件
        if (!File.Exists(templateZipPath))
            throw new FileNotFoundException($"Template file not found: {templateZipPath}");

        // 创建项目目录
        if (!Directory.Exists(projectPath))
            Directory.CreateDirectory(projectPath);

        // 解压模板（按 ZIP entry 逐文件补齐：缺失文件补全，已存在文件按合并策略处理）
        var state = await GetInitializationStateAsync(projectPath);
        await ExtractTemplateWithReplacementAsync(templateZipPath, projectPath, projectName, overwriteExisting: false);
        if (!state.Extracted)
        {
            state.Extracted = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 重命名文件和目录
        if (!state.Renamed)
        {
            await RenameFilesAndDirectoriesAsync(projectPath, projectName);
            state.Renamed = true;
            await SaveInitializationStateAsync(projectPath, state);
        }

        // 替换项目名称和命名空间
        if (!state.Replaced)
        {
            await ReplaceProjectNameAsync(projectPath, projectName);
            state.Replaced = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
        await RemoveClientShellBridgeFromGeneratedMainAsync(projectPath, projectName);
    }

    /// <summary>
    /// 步骤2：生成解决方案文件
    /// </summary>
    public async Task Step2_GenerateSolutionAsync(string projectName, string projectPath, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.SolutionGenerated)
        {
            await GenerateSolutionFileAsync(projectPath, projectName);
            state.SolutionGenerated = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤3：更新数据库配置
    /// </summary>
    public async Task Step3_UpdateDatabaseConfigAsync(string projectName, string projectPath, string databaseType, string connectionString, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.DatabaseConfigured)
        {
            await UpdateDatabaseConfigAsync(projectPath, projectName, databaseType, connectionString);
            state.DatabaseConfigured = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤4：更新端口配置
    /// </summary>
    public async Task Step4_UpdatePortConfigAsync(string projectName, string projectPath, int frontendPort, int backendPort, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.PortConfigured && (frontendPort > 0 || backendPort > 0))
        {
            await UpdatePortConfigAsync(projectPath, projectName, frontendPort, backendPort);
            state.PortConfigured = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤5：创建数据库迁移
    /// </summary>
    public async Task Step5_CreateMigrationAsync(string projectName, string projectPath, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.MigrationCreated)
        {
            // 创建迁移（不应用）
            var migratorPath = Path.Combine(projectPath, $"{projectName}.Migrator");
            // 先构建，使用初始化阶段写入到 csproj 的 DbProvider
            await RunProcessWithTimeoutAsync(
                "dotnet",
                "build",
                migratorPath,
                TimeSpan.FromMinutes(3));

            await RunProcessWithTimeoutAsync(
                "dotnet",
                "ef migrations add InitialCreate --no-build",
                migratorPath,
                TimeSpan.FromMinutes(2));

            state.MigrationCreated = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤6：应用数据库迁移
    /// </summary>
    public async Task Step6_ApplyMigrationAsync(string projectName, string projectPath, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.MigrationApplied)
        {
            await ApplyExistingDatabaseMigrationAsync(projectPath, projectName, projectId);
            state.MigrationApplied = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤7：运行 dotnet restore
    /// </summary>
    public async Task Step7_DotnetRestoreAsync(string projectName, string projectPath, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.DotnetRestored)
        {
            await RunDotnetRestoreAsync(projectPath, projectName);
            state.DotnetRestored = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤8：写入项目翻译
    /// </summary>
    public async Task Step8_WriteTranslationsAsync(string projectName, string projectPath, string displayName, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.TranslationsWritten && !string.IsNullOrEmpty(displayName))
        {
            await WriteProjectTranslationsAsync(projectPath, projectName, displayName);
            state.TranslationsWritten = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤9：运行 npm install
    /// </summary>
    public async Task Step9_NpmInstallAsync(string projectName, string projectPath, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.NpmInstalled)
        {
            await RunNpmInstallAsync(projectPath, projectName);
            state.NpmInstalled = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤10：启动后端服务
    /// </summary>
    public async Task Step10_StartBackendAsync(string projectName, string projectPath, int backendPort, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.BackendStarted && backendPort > 0)
        {
            await StartBackendAsync(projectPath, projectName, backendPort);
            state.BackendStarted = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    /// <summary>
    /// 步骤11：启动前端服务
    /// </summary>
    public async Task Step11_StartFrontendAsync(string projectName, string projectPath, int frontendPort, string projectId = null)
    {
        var state = await GetInitializationStateAsync(projectPath);
        if (!state.FrontendStarted && frontendPort > 0)
        {
            await StartFrontendAsync(projectPath, projectName, frontendPort);
            state.FrontendStarted = true;
            await SaveInitializationStateAsync(projectPath, state);
        }
    }

    #endregion
}
