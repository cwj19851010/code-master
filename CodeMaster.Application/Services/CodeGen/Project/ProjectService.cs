using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.DynamicApi;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using Mapster;
using SqlSugar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 项目管理服务
/// </summary>
public class ProjectService : CrudApplicationService<Project, ProjectDto, ProjectDto, ProjectQueryDto, CreateProjectDto, UpdateProjectDto>, IProjectService
{
    private readonly TemplateExportService _templateExportService;
    private readonly ProjectInitializationService _initializationService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlSugarClient _db;

    public ProjectService(
        IRepository<Project> repository,
        IExcelService excelService,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ISqlSugarClient db,
        CodeMaster.Core.Services.ICacheService? cacheService = null) : base(repository, excelService, cacheService)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _db = db;
        _templateExportService = new TemplateExportService();
        _initializationService = new ProjectInitializationService(configuration);
    }

    /// <summary>
    /// 导出模板
    /// </summary>
    public async Task<string> ExportTemplateAsync(string outputPath)
    {
        // 如果未指定输出路径，使用默认路径
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = null;
        }
        return await _templateExportService.ExportCleanTemplateAsync(outputPath);
    }

    /// <summary>
    /// 获取模板 Base64（用于客户端）
    /// </summary>
    public async Task<string> GetTemplateBase64Async()
    {
        // 1. 获取最新的模板文件
        var templateZipPath = GetTemplateZipPath();

        // 2. 读取文件并转换为 Base64
        var bytes = await File.ReadAllBytesAsync(templateZipPath);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 客户端初始化项目（用于客户端 WebView）
    /// </summary>
    public async Task<ClientInitializeProjectDto> GetClientInitializeDataAsync(long id)
    {
        // 1. 查询项目记录
        var project = await Repository.GetByIdAsync(id);
        if (project == null)
            throw new Exception("Project not found");

        // 2. 获取模板 Base64
        var templateBase64 = await GetTemplateBase64Async();

        // 3. 返回客户端初始化所需的数据
        return new ClientInitializeProjectDto
        {
            Id = project.Id,
            TemplateBase64 = templateBase64,
            ProjectName = project.ProjectName,
            ProjectPath = project.ProjectPath,
            DatabaseType = project.DatabaseType.ToString(),
            ConnectionString = project.ConnectionString
        };
    }

    /// <summary>
    /// Get the latest project template ZIP file for download.
    /// </summary>
    [DynamicApi(IsEnabled = false)]
    public async Task<ProjectTemplateFileDto> GetTemplateFileAsync()
    {
        var templateZipPath = GetTemplateZipPath();
        var bytes = await File.ReadAllBytesAsync(templateZipPath);

        return new ProjectTemplateFileDto
        {
            FileName = Path.GetFileName(templateZipPath),
            Content = bytes
        };
    }

    /// <summary>
    /// Save a new project template ZIP file for later initialization/download.
    /// </summary>
    [DynamicApi(IsEnabled = false)]
    public async Task<ProjectTemplateUploadResultDto> SaveTemplateFileAsync(Stream stream, string fileName, long length)
    {
        const long maxTemplateSize = 500L * 1024 * 1024;

        if (stream == null || length <= 0)
            throw new Exception("Template file cannot be empty");

        if (length > maxTemplateSize)
            throw new Exception("Template file cannot exceed 500 MB");

        if (!string.Equals(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Template file must be a .zip archive");

        var templatesDir = GetWritableTemplatesDirectory();
        Directory.CreateDirectory(templatesDir);

        var savedAt = DateTime.UtcNow;
        var savedFileName = $"CodeMaster_Template_{savedAt:yyyyMMddHHmmssfff}.zip";
        var savedPath = Path.Combine(templatesDir, savedFileName);

        await using (var fileStream = new FileStream(savedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream);
        }

        try
        {
            ValidateProjectTemplateZip(savedPath);
        }
        catch
        {
            File.Delete(savedPath);
            throw;
        }

        File.SetLastWriteTimeUtc(savedPath, savedAt);

        return new ProjectTemplateUploadResultDto
        {
            FileName = savedFileName,
            FileSize = new FileInfo(savedPath).Length,
            UploadedAt = savedAt
        };
    }

    /// <summary>
    /// 获取客户端本地执行代码生成所需的完整上下文快照。
    /// </summary>
    public async Task<GenerationBundleDto> GetGenerationBundleAsync(long id)
    {
        var project = await Repository.GetByIdAsync(id);
        if (project == null)
            throw new Exception("Project not found");

        var modules = await Repository.GetQueryable<ProjectModule>()
            .Where(m => m.ProjectId == id && m.IsDeleted == false)
            .OrderBy(m => m.OrderNum)
            .ToListAsync();

        var entities = await Repository.GetQueryable<ModuleEntity>()
            .Where(e => e.ProjectId == id && e.IsDeleted == false)
            .OrderBy(e => e.OrderNum)
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToList();
        var fields = new List<EntityField>();
        var relations = new List<OneToManyRelation>();

        if (entityIds.Count > 0)
        {
            fields = await Repository.GetQueryable<EntityField>()
                .Where(f => entityIds.Contains(f.ModuleEntityId) && f.IsDeleted == false)
                .OrderBy(f => f.OrderNum)
                .ToListAsync();

            relations = await Repository.GetQueryable<OneToManyRelation>()
                .Where(r => (entityIds.Contains(r.ModuleEntityId) || entityIds.Contains(r.ChildEntityId)) && r.IsDeleted == false)
                .OrderBy(r => r.OrderNum)
                .ToListAsync();
        }

        var bundle = new GenerationBundleDto
        {
            Project = project,
            Modules = modules,
            Entities = entities,
            Fields = fields,
            Relations = relations,
            PageTemplates = await Repository.GetQueryable<SysPageTemplate>()
                .Where(t => t.IsDeleted == false)
                .OrderBy(t => t.PageType)
                .OrderBy(t => t.Sort)
                .ToListAsync(),
            FieldControlTemplates = await Repository.GetQueryable<SysFieldControlTemplate>()
                .Where(t => t.IsDeleted == false)
                .OrderBy(t => t.ControlType)
                .OrderBy(t => t.PageSection)
                .OrderBy(t => t.Sort)
                .ToListAsync(),
            ChildTemplates = await Repository.GetQueryable<SysChildTemplate>()
                .Where(t => t.IsDeleted == false)
                .OrderBy(t => t.PageType)
                .OrderBy(t => t.ChildType)
                .OrderBy(t => t.Sort)
                .ToListAsync()
        };

        return bundle;
    }

    /// <summary>
    /// 客户端本地执行完成后回写服务器元数据状态。
    /// </summary>
    public async Task<bool> CompleteLocalExecutionAsync(LocalExecutionCompleteDto input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var action = input.Action?.Trim();
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(input));

        var completedAt = input.CompletedAt == default
            ? DateTime.UtcNow
            : input.CompletedAt.ToLocalTime();

        switch (action)
        {
            case "initializeProject":
            case "initializeStep11":
                await MarkProjectStatusAsync(input.ProjectId, Core.Enums.ProjectStatus.Initialized, completedAt);
                return true;

            case "startProject":
                await MarkProjectStatusAsync(input.ProjectId, Core.Enums.ProjectStatus.Running, completedAt);
                return true;

            case "stopProject":
                await MarkProjectStatusAsync(input.ProjectId, Core.Enums.ProjectStatus.Stopped, completedAt);
                return true;

            case "generateCode":
            case "generateIncrementalCode":
                await MarkGeneratedEntitiesAsync(input, completedAt);
                return true;

            case "syncModuleToMenu":
                await MarkModuleSyncedAsync(input.ModuleId, completedAt);
                return true;

            default:
                return true;
        }
    }

    private async Task MarkProjectStatusAsync(long? projectId, Core.Enums.ProjectStatus status, DateTime completedAt)
    {
        if (!projectId.HasValue || projectId.Value == 0)
            throw new ArgumentException("ProjectId is required");

        var project = await Repository.GetByIdAsync(projectId.Value);
        if (project == null)
            throw new Exception("Project not found");

        project.Status = status;
        if (status == Core.Enums.ProjectStatus.Initialized)
        {
            project.InitializedAt = completedAt;
            project.InitError = null;
        }

        await Repository.UpdateAsync(project);
    }

    private async Task MarkGeneratedEntitiesAsync(LocalExecutionCompleteDto input, DateTime completedAt)
    {
        var entityIds = await ResolveGeneratedEntityIdsAsync(input);
        if (entityIds.Count == 0)
            throw new ArgumentException("EntityId is required");

        var entityIdList = entityIds.ToList();
        var entities = await _db.Queryable<ModuleEntity>()
            .Where(e => entityIdList.Contains(e.Id) && e.IsDeleted == false)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsGenerated = true;
            entity.LastGeneratedTime = completedAt;
            await _db.Updateable(entity).ExecuteCommandAsync();
        }
    }

    private async Task<HashSet<long>> ResolveGeneratedEntityIdsAsync(LocalExecutionCompleteDto input)
    {
        var result = input.EntityIds.Where(id => id > 0).ToHashSet();
        if (input.EntityId.HasValue && input.EntityId.Value > 0)
            result.Add(input.EntityId.Value);

        if (result.Count == 0)
            return result;

        var knownEntities = await _db.Queryable<ModuleEntity>()
            .Where(e => result.Contains(e.Id) && e.IsDeleted == false)
            .ToListAsync();

        if (knownEntities.Count == 0)
            throw new Exception("Entity not found");

        if (input.ProjectId.HasValue && input.ProjectId.Value > 0 &&
            knownEntities.Any(e => e.ProjectId != input.ProjectId.Value))
        {
            throw new Exception("Entity does not belong to the project");
        }

        var projectId = input.ProjectId.GetValueOrDefault(knownEntities.First().ProjectId);
        var projectEntityIds = await _db.Queryable<ModuleEntity>()
            .Where(e => e.ProjectId == projectId && e.IsDeleted == false)
            .Select(e => e.Id)
            .ToListAsync();

        var relations = await _db.Queryable<OneToManyRelation>()
            .Where(r => r.IsDeleted == false &&
                (projectEntityIds.Contains(r.ModuleEntityId) || projectEntityIds.Contains(r.ChildEntityId)))
            .ToListAsync();

        var queue = new Queue<long>(result);
        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var childId in relations.Where(r => r.ModuleEntityId == parentId).Select(r => r.ChildEntityId))
            {
                if (childId <= 0 || !result.Add(childId))
                    continue;

                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private async Task MarkModuleSyncedAsync(long? moduleId, DateTime completedAt)
    {
        if (!moduleId.HasValue || moduleId.Value == 0)
            throw new ArgumentException("ModuleId is required");

        var module = await _db.Queryable<ProjectModule>()
            .Where(m => m.Id == moduleId.Value && m.IsDeleted == false)
            .FirstAsync();

        if (module == null)
            throw new Exception("Module not found");

        module.IsSynced = true;
        module.LastSyncTime = completedAt;
        await _db.Updateable(module).ExecuteCommandAsync();
    }

    /// <summary>
    /// 创建查询
    /// </summary>
    protected override async Task<ISugarQueryable<Project>> CreateFilteredQueryAsync(ProjectQueryDto? input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (input == null)
            return query;

        if (!string.IsNullOrWhiteSpace(input.ProjectName))
            query = query.Where(x => x.ProjectName.Contains(input.ProjectName));

        if (!string.IsNullOrWhiteSpace(input.DisplayName))
            query = query.Where(x => x.DisplayName.Contains(input.DisplayName));

        if (input.DatabaseType.HasValue)
            query = query.Where(x => x.DatabaseType == input.DatabaseType.Value);

        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        return query;
    }

    /// <summary>
    /// 初始化项目
    /// </summary>
    [DynamicApi(IsEnabled = false)]
    public async Task<bool> InitializeAsync(InitializeProjectDto input)
    {
        Console.WriteLine($"[InitializeAsync] Received input: Id={input?.Id}, TargetPath={input?.TargetPath}");

        if (input == null)
            throw new ArgumentNullException(nameof(input), "Input cannot be null");

        if (input.Id == 0)
            throw new ArgumentException("Project ID cannot be 0", nameof(input.Id));

        // 直接查询，避免 Repository 的过滤器
        var project = await Repository.GetByIdAsync(input.Id);
        if (project == null)
            throw new Exception("Project not found");

        //if (project.Status == Core.Enums.ProjectStatus.Initialized)
        //    throw new Exception("Project already initialized");

        try
        {
            // 设置进度回调
            _initializationService.SetProgressCallback((projectId, step, message, progress) =>
            {
                try
                {
                    // 使用反射获取 IHubContext（避免直接引用 Microsoft.AspNetCore.SignalR）
                    var hubContextType = Type.GetType("Microsoft.AspNetCore.SignalR.IHubContext`1, Microsoft.AspNetCore.SignalR");
                    if (hubContextType != null)
                    {
                        var hubType = Type.GetType("CodeMaster.WebApi.Hubs.ProjectInitializationHub, CodeMaster.WebApi");
                        if (hubType != null)
                        {
                            var genericHubContextType = hubContextType.MakeGenericType(hubType);
                            var hubContext = _serviceProvider.GetService(genericHubContextType);

                            if (hubContext != null)
                            {
                                var clientsProperty = genericHubContextType.GetProperty("Clients");
                                var clients = clientsProperty?.GetValue(hubContext);

                                if (clients != null)
                                {
                                    var allProperty = clients.GetType().GetProperty("All");
                                    var allClients = allProperty?.GetValue(clients);

                                    if (allClients != null)
                                    {
                                        var sendAsyncMethod = allClients.GetType().GetMethod("SendAsync");
                                        sendAsyncMethod?.Invoke(allClients, new object[] { "ReceiveProgress", new object[] { projectId, step, message, progress }, default(CancellationToken) });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SignalR Error] {ex.Message}");
                }
            });

            // 1. 获取最新的模板文件
        var templateZipPath = GetTemplateZipPath();

            // 使用传入的 TargetPath 或项目记录中的 ProjectPath
            var targetPath = input.TargetPath ?? project.ProjectPath;
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new Exception("Target path is required");

            // 检查路径是否已经包含项目名称（避免重复添加）
            var fullProjectPath = targetPath;
            if (!targetPath.EndsWith(project.ProjectName, StringComparison.OrdinalIgnoreCase))
            {
                // 如果路径不以项目名称结尾，则添加项目名称
                fullProjectPath = Path.Combine(targetPath, project.ProjectName);
            }

            // 2. 初始化项目
            await _initializationService.InitializeProjectAsync(
                project.ProjectName,
                fullProjectPath,
                project.DatabaseType.ToString(),
                project.ConnectionString,
                templateZipPath,
                project.FrontendPort ?? 0,
                project.BackendPort ?? 0,
                project.Id.ToString(),
                project.DisplayName);

            // 注意：数据库迁移、npm install、服务启动已在 InitializeProjectAsync 中完成
            // 不需要再单独调用

            project.Status = Core.Enums.ProjectStatus.Initialized;
            project.ProjectPath = fullProjectPath; // 更新为完整路径
            project.InitializedAt = DateTime.UtcNow;
            project.InitError = null;

            await Repository.UpdateAsync(project);
            return true;
        }
        catch (Exception ex)
        {
            project.Status = Core.Enums.ProjectStatus.InitializeFailed;
            project.InitError = ex.Message;
            await Repository.UpdateAsync(project);
            throw;
        }
    }

    /// <summary>
    /// 启动项目
    /// </summary>
    public async Task<bool> StartAsync(long id)
    {
        var project = await Repository.GetByIdAsync(id);
        if (project == null)
            throw new Exception("Project not found");

        if (project.Status != Core.Enums.ProjectStatus.Initialized &&
            project.Status != Core.Enums.ProjectStatus.Stopped)
            throw new Exception("Project cannot be started");

        // TODO: 实现项目启动逻辑
        // 1. 启动后端服务
        // 2. 启动前端服务

        project.Status = Core.Enums.ProjectStatus.Running;
        await Repository.UpdateAsync(project);
        return true;
    }

    /// <summary>
    /// 停止项目
    /// </summary>
    public async Task<bool> StopAsync(long id)
    {
        var project = await Repository.GetByIdAsync(id);
        if (project == null)
            throw new Exception("Project not found");

        if (project.Status != Core.Enums.ProjectStatus.Running)
            throw new Exception("Project is not running");

        // TODO: 实现项目停止逻辑
        // 1. 停止前端服务
        // 2. 停止后端服务

        project.Status = Core.Enums.ProjectStatus.Stopped;
        await Repository.UpdateAsync(project);
        return true;
    }

    #region 分步初始化方法

    /// <summary>
    /// 获取项目完整路径
    /// </summary>
    private string GetFullProjectPath(Domain.Entities.CodeGen.Project project, string? targetPath)
    {
        var path = targetPath ?? project.ProjectPath;
        if (string.IsNullOrWhiteSpace(path))
            throw new Exception("Target path is required");

        // 检查路径是否已经包含项目名称
        if (!path.EndsWith(project.ProjectName, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(path, project.ProjectName);
        }
        return path;
    }

    /// <summary>
    /// 获取模板文件路径
    /// </summary>
    private string GetTemplateZipPath()
    {
        var configuredPath = _configuration["TemplatesPath"] ?? _configuration["CodeGen:TemplatesPath"];
        var templatesDir = ResolveTemplatesDirectory(configuredPath);

        if (!Directory.Exists(templatesDir))
            throw new Exception($"Templates directory not found: {templatesDir}");

        var templateFiles = Directory.GetFiles(templatesDir, "CodeMaster_Template_*.zip")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();

        if (!templateFiles.Any())
            throw new Exception($"No template file found in {templatesDir}");

        return templateFiles.First();
    }

    private static string ResolveTemplatesDirectory(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
            return Directory.Exists(fullPath)
                ? fullPath
                : throw new Exception($"Configured TemplatesPath directory not found: {fullPath}");
        }

        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var templatesDir = FindTemplatesDirectoryUpwards(startPath);
            if (templatesDir != null)
                return templatesDir;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "Templates");
    }

    private string GetWritableTemplatesDirectory()
    {
        var configuredPath = _configuration["TemplatesPath"] ?? _configuration["CodeGen:TemplatesPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
        }

        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var templatesDir = FindTemplatesDirectoryUpwards(startPath);
            if (templatesDir != null)
                return templatesDir;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "Templates");
    }

    private static void ValidateProjectTemplateZip(string templatePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(templatePath);
            var entries = archive.Entries
                .Select(e => NormalizeZipEntryName(e.FullName))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (!entries.Contains("CodeMaster.sln", StringComparer.OrdinalIgnoreCase))
                throw new Exception("Template ZIP must contain CodeMaster.sln at the root");

            if (!entries.Any(e => e.StartsWith("CodeMaster.WebApi/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.WebApi");

            if (!entries.Any(e => e.StartsWith("CodeMaster.Vue/", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Template ZIP must contain CodeMaster.Vue");
        }
        catch (InvalidDataException ex)
        {
            throw new Exception("Template file is not a valid ZIP archive", ex);
        }
    }

    private static string NormalizeZipEntryName(string entryName)
    {
        return entryName.Replace('\\', '/').TrimStart('/');
    }

    private static string? FindTemplatesDirectoryUpwards(string startPath)
    {
        var directory = Directory.Exists(startPath)
            ? new DirectoryInfo(startPath)
            : new DirectoryInfo(Path.GetDirectoryName(startPath) ?? startPath);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Templates");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
    }

    public async Task<InitializeStepResultDto> Step1_ExtractTemplateAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);
            var templateZipPath = GetTemplateZipPath();

            await _initializationService.Step1_ExtractTemplateAsync(
                project.ProjectName, projectPath, templateZipPath, project.Id.ToString());

            // 更新项目路径
            project.ProjectPath = projectPath;
            await Repository.UpdateAsync(project);

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "模板解压完成",
                Step = "extract_template",
                Progress = 20
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "模板解压失败",
                Step = "extract_template",
                Progress = 0,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step2_GenerateSolutionAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step2_GenerateSolutionAsync(
                project.ProjectName, projectPath, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "解决方案文件生成完成",
                Step = "generate_solution",
                Progress = 30
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "解决方案文件生成失败",
                Step = "generate_solution",
                Progress = 20,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step3_UpdateDatabaseConfigAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step3_UpdateDatabaseConfigAsync(
                project.ProjectName, projectPath, project.DatabaseType.ToString(),
                project.ConnectionString, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "数据库配置更新完成",
                Step = "update_database_config",
                Progress = 40
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "数据库配置更新失败",
                Step = "update_database_config",
                Progress = 30,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step4_UpdatePortConfigAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step4_UpdatePortConfigAsync(
                project.ProjectName, projectPath, project.FrontendPort ?? 0,
                project.BackendPort ?? 0, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "端口配置更新完成",
                Step = "update_port_config",
                Progress = 50
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "端口配置更新失败",
                Step = "update_port_config",
                Progress = 40,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step5_CreateMigrationAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step5_CreateMigrationAsync(
                project.ProjectName, projectPath, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "数据库迁移创建完成",
                Step = "create_migration",
                Progress = 60
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "数据库迁移创建失败",
                Step = "create_migration",
                Progress = 50,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step6_ApplyMigrationAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            Console.WriteLine($"[Step6] Starting migration for project: {project.ProjectName}");
            Console.WriteLine($"[Step6] Project path: {projectPath}");

            await _initializationService.Step6_ApplyMigrationAsync(
                project.ProjectName, projectPath, project.Id.ToString());

            Console.WriteLine($"[Step6] Migration completed successfully");

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "数据库迁移应用完成",
                Step = "apply_migration",
                Progress = 70
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Step6] Migration failed: {ex.Message}");
            Console.WriteLine($"[Step6] Stack trace: {ex.StackTrace}");

            return new InitializeStepResultDto
            {
                Success = false,
                Message = "数据库迁移应用失败",
                Step = "apply_migration",
                Progress = 60,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step7_DotnetRestoreAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step7_DotnetRestoreAsync(
                project.ProjectName, projectPath, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "后端依赖还原完成",
                Step = "dotnet_restore",
                Progress = 75
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "后端依赖还原失败",
                Step = "dotnet_restore",
                Progress = 70,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step8_WriteTranslationsAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step8_WriteTranslationsAsync(
                project.ProjectName, projectPath, project.DisplayName, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "项目翻译写入完成",
                Step = "write_translations",
                Progress = 80
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "项目翻译写入失败",
                Step = "write_translations",
                Progress = 75,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step9_NpmInstallAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            await _initializationService.Step9_NpmInstallAsync(
                project.ProjectName, projectPath, project.Id.ToString());

            return new InitializeStepResultDto
            {
                Success = true,
                Message = "前端依赖安装完成",
                Step = "npm_install",
                Progress = 90
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "前端依赖安装失败",
                Step = "npm_install",
                Progress = 80,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step10_StartBackendAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            var result = await ProjectProcessLauncher.StartBackendAsync(project.ProjectName, projectPath, project.BackendPort);
            if (!result.Success)
                throw new Exception(result.Message);

            return new InitializeStepResultDto
            {
                Success = true,
                Message = result.Message,
                Step = "start_backend",
                Progress = 95
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "后端服务启动失败",
                Step = "start_backend",
                Progress = 90,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializeStepResultDto> Step11_StartFrontendAsync(InitializeStepDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, input.TargetPath);

            var result = await ProjectProcessLauncher.StartFrontendAsync(project.ProjectName, projectPath, project.FrontendPort);
            if (!result.Success)
                throw new Exception(result.Message);

            // 更新项目状态为已初始化
            project.Status = Core.Enums.ProjectStatus.Initialized;
            project.InitializedAt = DateTime.UtcNow;
            project.InitError = null;
            await Repository.UpdateAsync(project);

            return new InitializeStepResultDto
            {
                Success = true,
                Message = result.Message,
                Step = "start_frontend",
                Progress = 100
            };
        }
        catch (Exception ex)
        {
            return new InitializeStepResultDto
            {
                Success = false,
                Message = "前端服务启动失败",
                Step = "start_frontend",
                Progress = 95,
                Error = ex.Message
            };
        }
    }

    public async Task<InitializationStateDto> GetInitializationStateAsync(long projectId)
    {
        var project = await Repository.GetByIdAsync(projectId);
        if (project == null)
            throw new Exception("Project not found");

        var projectPath = GetFullProjectPath(project, null);
        var state = await _initializationService.GetInitializationStateAsync(projectPath);

        return new InitializationStateDto
        {
            Extracted = state.Extracted,
            Renamed = state.Renamed,
            Replaced = state.Replaced,
            SolutionGenerated = state.SolutionGenerated,
            DatabaseConfigured = state.DatabaseConfigured,
            PortConfigured = state.PortConfigured,
            MigrationCreated = state.MigrationCreated,
            MigrationApplied = state.MigrationApplied,
            DotnetRestored = state.DotnetRestored,
            TranslationsWritten = state.TranslationsWritten,
            NpmInstalled = state.NpmInstalled,
            BackendStarted = state.BackendStarted,
            FrontendStarted = state.FrontendStarted
        };
    }

    #endregion

    #region 项目管理方法

    public async Task<ProjectActionResultDto> StartFrontendAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, null);
            return await ProjectProcessLauncher.StartFrontendAsync(project.ProjectName, projectPath, project.FrontendPort);
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"前端服务启动失败: {ex.Message}"
            };
        }
    }

    public async Task<ProjectActionResultDto> StartBackendAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, null);
            return await ProjectProcessLauncher.StartBackendAsync(project.ProjectName, projectPath, project.BackendPort);
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"后端服务启动失败: {ex.Message}"
            };
        }
    }

    public async Task<ProjectActionResultDto> StopFrontendAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            if (!project.FrontendPort.HasValue || project.FrontendPort.Value == 0)
            {
                return new ProjectActionResultDto
                {
                    Success = false,
                    Message = "前端端口未配置"
                };
            }

            var port = project.FrontendPort.Value;
            var processIds = await GetProcessIdsByPortAsync(port);

            if (processIds.Count == 0)
            {
                return new ProjectActionResultDto
                {
                    Success = false,
                    Message = $"未找到占用端口 {port} 的进程"
                };
            }

            var killedCount = 0;
            foreach (var pid in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(pid);
                    process.Kill(entireProcessTree: true);
                    killedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill process {pid}: {ex.Message}");
                }
            }

            return new ProjectActionResultDto
            {
                Success = true,
                Message = $"前端服务停止成功，已终止 {killedCount} 个进程"
            };
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"前端服务停止失败: {ex.Message}"
            };
        }
    }

    public async Task<ProjectActionResultDto> StopBackendAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            if (!project.BackendPort.HasValue || project.BackendPort.Value == 0)
            {
                return new ProjectActionResultDto
                {
                    Success = false,
                    Message = "后端端口未配置"
                };
            }

            var port = project.BackendPort.Value;
            var processIds = await GetProcessIdsByPortAsync(port);

            if (processIds.Count == 0)
            {
                return new ProjectActionResultDto
                {
                    Success = false,
                    Message = $"未找到占用端口 {port} 的进程"
                };
            }

            var killedCount = 0;
            foreach (var pid in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(pid);
                    process.Kill(entireProcessTree: true);
                    killedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill process {pid}: {ex.Message}");
                }
            }

            return new ProjectActionResultDto
            {
                Success = true,
                Message = $"后端服务停止成功，已终止 {killedCount} 个进程"
            };
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"后端服务停止失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 根据端口获取进程ID列表
    /// </summary>
    private async Task<List<int>> GetProcessIdsByPortAsync(int port)
    {
        var processIds = new List<int>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return processIds;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // 解析 netstat 输出
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains($":{port} ") || line.Contains($":{port}\t"))
                {
                    // 提取进程ID（最后一列）
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5 && int.TryParse(parts[^1], out var pid))
                    {
                        if (!processIds.Contains(pid))
                        {
                            processIds.Add(pid);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get process IDs by port: {ex.Message}");
        }

        return processIds;
    }

    public async Task<ProjectStatusDto> GetStatusAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            // TODO: 实现状态查询逻辑
            await Task.CompletedTask;

            return new ProjectStatusDto
            {
                FrontendRunning = false,
                BackendRunning = false
            };
        }
        catch (Exception)
        {
            return new ProjectStatusDto
            {
                FrontendRunning = false,
                BackendRunning = false
            };
        }
    }

    public async Task<ProjectActionResultDto> MigrateDatabaseAsync(ProjectActionDto input)
    {
        var outputBuilder = new global::System.Text.StringBuilder();
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, null);
            var migratorPath = Path.Combine(projectPath, $"{project.ProjectName}.Migrator");

            if (!Directory.Exists(migratorPath))
                throw new Exception($"Migrator 项目不存在: {migratorPath}");

            var migrationName = $"Auto{DateTime.UtcNow:yyyyMMddHHmmss}";

            // 1. 构建 Migrator 项目
            outputBuilder.AppendLine($"[1/3] 构建 {project.ProjectName}.Migrator...");
            await RunCommandAsync("dotnet", "build", migratorPath, outputBuilder, TimeSpan.FromMinutes(3));

            // 2. 创建迁移
            outputBuilder.AppendLine($"[2/3] 创建迁移 {migrationName}...");
            await RunCommandAsync("dotnet", $"ef migrations add {migrationName} --no-build", migratorPath, outputBuilder, TimeSpan.FromMinutes(2));

            // 3. 运行 Migrator 应用迁移
            outputBuilder.AppendLine($"[3/3] 应用数据库迁移...");
            await RunCommandAsync("dotnet", "run", migratorPath, outputBuilder, TimeSpan.FromMinutes(5));

            return new ProjectActionResultDto
            {
                Success = true,
                Message = $"数据库迁移成功（{migrationName}）",
                Output = outputBuilder.ToString()
            };
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"数据库迁移失败: {ex.Message}",
                Output = outputBuilder.ToString()
            };
        }
    }

    public async Task<List<DictTypeOptionDto>> GetDictTypesAsync(long projectId)
    {
        var project = await Repository.GetByIdAsync(projectId);
        if (project == null)
            throw new Exception("项目不存在");

        var connectionString = NormalizeConnectionString(project);
        var dbType = GetTargetDbType(project.DatabaseType);
        var targetDb = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(dbType)
        });

        var dictTypes = await targetDb.Queryable<CodeMaster.Domain.Entities.System.SysDictType>()
            .Where(t => t.Status == 0)
            .OrderBy(t => t.Sort)
            .ToListAsync();

        return dictTypes.Select(t => new DictTypeOptionDto
        {
            DictType = t.DictType,
            DictName = t.DictName
        }).ToList();
    }

    private string NormalizeConnectionString(Project project)
    {
        if (project.DatabaseType != Core.Enums.DatabaseType.SQLite)
            return project.ConnectionString ?? string.Empty;

        var connectionString = project.ConnectionString ?? string.Empty;
        var match = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return connectionString;

        var dataSource = match.Groups[1].Value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dataSource) || Path.IsPathRooted(dataSource))
            return connectionString;

        var projectPath = GetFullProjectPath(project, null);
        var absolutePath = Path.GetFullPath(Path.Combine(projectPath, $"{project.ProjectName}.Migrator", dataSource));
        return Regex.Replace(connectionString, @"Data Source\s*=\s*([^;]+)", "Data Source=" + absolutePath, RegexOptions.IgnoreCase);
    }

    private DbType GetTargetDbType(Core.Enums.DatabaseType databaseType)
    {
        return databaseType switch
        {
            Core.Enums.DatabaseType.MySQL => DbType.MySql,
            Core.Enums.DatabaseType.SqlServer => DbType.SqlServer,
            Core.Enums.DatabaseType.PostgreSQL => DbType.PostgreSQL,
            Core.Enums.DatabaseType.SQLite => DbType.Sqlite,
            Core.Enums.DatabaseType.Oracle => DbType.Oracle,
            _ => DbType.SqlServer
        };
    }

    private async Task RunCommandAsync(string fileName, string arguments, string workingDirectory, global::System.Text.StringBuilder outputBuilder, TimeSpan timeout)
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

        Console.WriteLine($"[Migrate] {fileName} {arguments} (cwd: {workingDirectory})");

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new Exception($"无法启动进程: {fileName} {arguments}");

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Migrate Output] {e.Data}");
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Migrate Error] {e.Data}");
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cts.Token);

        if (process.ExitCode != 0)
        {
            throw new Exception($"{fileName} {arguments} 退出码: {process.ExitCode}");
        }
    }

    public async Task<ProjectActionResultDto> BuildAsync(ProjectActionDto input)
    {
        try
        {
            var project = await Repository.GetByIdAsync(input.ProjectId);
            if (project == null)
                throw new Exception("Project not found");

            var projectPath = GetFullProjectPath(project, null);

            // TODO: 实现项目编译逻辑
            await Task.CompletedTask;

            return new ProjectActionResultDto
            {
                Success = true,
                Message = "项目编译成功"
            };
        }
        catch (Exception ex)
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"项目编译失败: {ex.Message}"
            };
        }
    }

    #endregion
}
