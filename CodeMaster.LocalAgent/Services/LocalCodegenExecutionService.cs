using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Enums;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using CodeMaster.LocalAgent.Models;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace CodeMaster.LocalAgent.Services;

public class LocalCodegenExecutionService
{
    private readonly CodeMasterServerClient _serverClient;
    private readonly LocalMetadataStore _metadataStore;
    private readonly ProjectInitializationService _initializationService;

    public LocalCodegenExecutionService(
        CodeMasterServerClient serverClient,
        LocalMetadataStore metadataStore,
        IConfiguration configuration,
        IOptions<LocalAgentOptions> options)
    {
        _serverClient = serverClient;
        _metadataStore = metadataStore;
        _initializationService = new ProjectInitializationService(configuration);
    }

    public async Task<LocalExecutionResult> ExecuteAsync(string routeAction, LocalExecutionRequest request)
    {
        var action = string.IsNullOrWhiteSpace(request.Action) ? routeAction : request.Action;

        try
        {
            var result = action switch
            {
                "initializeProject" => await InitializeProjectAsync(request),
                "initializeStep1" => await RunInitializationStepAsync(request, 1),
                "initializeStep2" => await RunInitializationStepAsync(request, 2),
                "initializeStep3" => await RunInitializationStepAsync(request, 3),
                "initializeStep4" => await RunInitializationStepAsync(request, 4),
                "initializeStep5" => await RunInitializationStepAsync(request, 5),
                "initializeStep6" => await RunInitializationStepAsync(request, 6),
                "initializeStep7" => await RunInitializationStepAsync(request, 7),
                "initializeStep8" => await RunInitializationStepAsync(request, 8),
                "initializeStep9" => await RunInitializationStepAsync(request, 9),
                "initializeStep10" => await RunInitializationStepAsync(request, 10),
                "initializeStep11" => await RunInitializationStepAsync(request, 11),
                "generateCode" => await GenerateCodeAsync(request),
                "generateIncrementalCode" => await GenerateIncrementalCodeAsync(request),
                "generateProjectCode" => await GenerateProjectCodeAsync(request, incremental: false),
                "generateProjectIncrementalCode" => await GenerateProjectCodeAsync(request, incremental: true),
                "syncMenu" => await SyncMenuAsync(request),
                "syncLanguage" => await SyncLanguageAsync(request),
                "syncProjectMenus" => await SyncProjectMenusAsync(request),
                "syncProjectLanguages" => await SyncProjectLanguagesAsync(request),
                "syncModuleToMenu" => await SyncModuleToMenuAsync(request),
                "getPageContent" => await GetPageContentAsync(request),
                "savePageContent" => await SavePageContentAsync(request),
                "getPageScript" => await GetPageScriptAsync(request),
                "savePageScript" => await SavePageScriptAsync(request),
                "getFieldScripts" => await GetFieldScriptsAsync(request),
                "saveFieldScripts" => await SaveFieldScriptsAsync(request),
                "getDictTypes" => await GetDictTypesAsync(request),
                "migrateDatabase" => await MigrateDatabaseAsync(request),
                "buildProject" => await BuildProjectAsync(request),
                "buildFrontend" => await BuildFrontendAsync(request),
                "enhanceUiPage" => await EnhanceUiPageAsync(request),
                "startFrontend" => await StartFrontendAsync(request),
                "startBackend" => await StartBackendAsync(request),
                "startProject" => await StartProjectAsync(request),
                "stopFrontend" => await StopProcessAsync(request, "frontend"),
                "stopBackend" => await StopProcessAsync(request, "backend"),
                "stopProject" => await StopProjectAsync(request),
                "getProjectStatus" => await GetProjectStatusAsync(request),
                "downloadTemplate" => await DownloadTemplateAsync(request),
                _ => LocalExecutionResult.Fail($"Unsupported local action: {action}")
            };

            if (result.Success)
            {
                await TryCompleteServerMetadataAsync(action, request, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            return LocalExecutionResult.Fail(FormatExceptionMessage(ex), ex.ToString());
        }
    }

    private async Task<LocalExecutionResult> InitializeProjectAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        var project = bundle.Project;
        ApplyProjectOverrides(project, request.Payload);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var localTemplateZipPath = FindLocalTemplateZipPath();

        if (!string.IsNullOrWhiteSpace(localTemplateZipPath))
        {
            await _initializationService.InitializeProjectAsync(
                project.ProjectName,
                projectPath,
                project.DatabaseType.ToString(),
                project.ConnectionString,
                localTemplateZipPath,
                project.FrontendPort ?? 0,
                project.BackendPort ?? 0,
                project.Id.ToString(),
                project.DisplayName);
        }
        else
        {
            var data = await _serverClient.GetClientInitializeDataAsync(request.ServerBaseUrl, projectId, request.AccessToken);
            await _initializationService.InitializeProjectFromBase64Async(
                project.ProjectName,
                projectPath,
                project.DatabaseType.ToString(),
                project.ConnectionString,
                data.TemplateBase64,
                project.FrontendPort ?? 0,
                project.BackendPort ?? 0);
        }

        await _initializationService.WriteProjectContextAsync(
            projectPath,
            project.ProjectName,
            project.Id.ToString(),
            project.DisplayName,
            project.DatabaseType.ToString(),
            project.FrontendPort ?? 0,
            project.BackendPort ?? 0,
            request.ServerBaseUrl);

        return LocalExecutionResult.Ok("项目初始化完成", new InitializeStepResultDto
        {
            Success = true,
            Message = "项目初始化完成",
            Step = "initialize_project",
            Progress = 100
        });
    }

    private async Task<LocalExecutionResult> RunInitializationStepAsync(LocalExecutionRequest request, int step)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        var project = bundle.Project;
        ApplyProjectOverrides(project, request.Payload);
        var projectPath = GetFullProjectPath(project.ProjectName, GetString(request.Payload, "targetPath") ?? project.ProjectPath);
        var projectIdText = project.Id.ToString();

        switch (step)
        {
            case 1:
                var templateZipPath = FindLocalTemplateZipPath();
                if (string.IsNullOrWhiteSpace(templateZipPath))
                {
                    var initData = await _serverClient.GetClientInitializeDataAsync(request.ServerBaseUrl, projectId, request.AccessToken);
                    templateZipPath = await SaveTemplateZipAsync(projectId, initData.TemplateBase64);
                }
                await _initializationService.Step1_ExtractTemplateAsync(project.ProjectName, projectPath, templateZipPath, projectIdText);
                return StepOk("模板解压完成", "extract_template", 20);
            case 2:
                await _initializationService.Step2_GenerateSolutionAsync(project.ProjectName, projectPath, projectIdText);
                return StepOk("解决方案生成完成", "generate_solution", 30);
            case 3:
                await _initializationService.Step3_UpdateDatabaseConfigAsync(project.ProjectName, projectPath, project.DatabaseType.ToString(), project.ConnectionString, projectIdText);
                return StepOk("数据库配置更新完成", "update_database_config", 40);
            case 4:
                await _initializationService.Step4_UpdatePortConfigAsync(project.ProjectName, projectPath, project.FrontendPort ?? 0, project.BackendPort ?? 0, projectIdText);
                return StepOk("端口配置更新完成", "update_port_config", 50);
            case 5:
                await _initializationService.Step5_CreateMigrationAsync(project.ProjectName, projectPath, projectIdText);
                return StepOk("数据库迁移创建完成", "create_migration", 60);
            case 6:
                await _initializationService.Step6_ApplyMigrationAsync(project.ProjectName, projectPath, projectIdText);
                return StepOk("数据库迁移应用完成", "apply_migration", 70);
            case 7:
                await _initializationService.Step7_DotnetRestoreAsync(project.ProjectName, projectPath, projectIdText);
                return StepOk("dotnet restore 完成", "dotnet_restore", 75);
            case 8:
                await _initializationService.Step8_WriteTranslationsAsync(project.ProjectName, projectPath, project.DisplayName, projectIdText);
                return StepOk("项目翻译写入完成", "write_translations", 80);
            case 9:
                var npmOutput = await RunCommandAsync(ProjectProcessLauncher.NpmCommand(), new[] { "install", "--legacy-peer-deps" }, Path.Combine(projectPath, $"{project.ProjectName}.Vue"), TimeSpan.FromMinutes(5));
                return StepOk("npm install 完成", "npm_install", 90, npmOutput);
            case 10:
                var backendStart = await ProjectProcessLauncher.StartBackendAsync(project.ProjectName, projectPath, project.BackendPort);
                return backendStart.Success
                    ? StepOk(backendStart.Message, "start_backend", 95, backendStart.Output)
                    : LocalExecutionResult.Fail(backendStart.Message, backendStart.Output);
            case 11:
                var frontendStart = await ProjectProcessLauncher.StartFrontendAsync(project.ProjectName, projectPath, project.FrontendPort);
                if (!frontendStart.Success)
                    return LocalExecutionResult.Fail(frontendStart.Message, frontendStart.Output);

                await _initializationService.WriteProjectContextAsync(
                    projectPath,
                    project.ProjectName,
                    project.Id.ToString(),
                    project.DisplayName,
                    project.DatabaseType.ToString(),
                    project.FrontendPort ?? 0,
                    project.BackendPort ?? 0,
                    request.ServerBaseUrl);

                return StepOk(frontendStart.Message, "start_frontend", 100, frontendStart.Output);
            default:
                return LocalExecutionResult.Fail($"Unsupported initialization step: {step}");
        }
    }

    private async Task<LocalExecutionResult> GenerateCodeAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        await context.ModuleEntityService.GenerateCodeAsync(entityId);
        return LocalExecutionResult.Ok("代码生成完成");
    }

    private async Task<LocalExecutionResult> GenerateIncrementalCodeAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        await context.ModuleEntityService.GenerateIncrementalCodeAsync(entityId);
        return LocalExecutionResult.Ok("增量生成完成");
    }

    private async Task<LocalExecutionResult> GenerateProjectCodeAsync(LocalExecutionRequest request, bool incremental)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var entityIds = GetLongList(request.Payload, "entityIds");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var input = new ProjectCodeGenerationDto
        {
            ProjectId = projectId,
            EntityIds = entityIds
        };
        if (incremental)
            await context.ModuleEntityService.GenerateProjectIncrementalCodeAsync(input);
        else
            await context.ModuleEntityService.GenerateProjectCodeAsync(input);

        var message = incremental && input.EntityIds.Count == 0
            ? "未检测到需要增量生成的实体"
            : incremental
                ? $"项目增量生成完成，共处理 {input.EntityIds.Count} 个实体"
                : $"项目全量重新生成完成，共处理 {input.EntityIds.Count} 个实体";
        return LocalExecutionResult.Ok(message, new { entityIds = input.EntityIds });
    }

    private async Task<LocalExecutionResult> SyncMenuAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        await context.ModuleEntityService.SyncMenuToTargetAsync(entityId);
        return LocalExecutionResult.Ok("菜单同步完成");
    }

    private async Task<LocalExecutionResult> SyncLanguageAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        await context.ModuleEntityService.SyncLanguageToTargetAsync(entityId);
        return LocalExecutionResult.Ok("多语言同步完成");
    }

    private async Task<LocalExecutionResult> SyncProjectMenusAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var input = new ProjectCodeGenerationDto
        {
            ProjectId = projectId,
            EntityIds = GetLongList(request.Payload, "entityIds")
        };
        await context.ModuleEntityService.SyncProjectMenusToTargetAsync(input);
        return LocalExecutionResult.Ok($"项目菜单同步完成，共处理 {input.EntityIds.Count} 个实体");
    }

    private async Task<LocalExecutionResult> SyncProjectLanguagesAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var input = new ProjectCodeGenerationDto
        {
            ProjectId = projectId,
            EntityIds = GetLongList(request.Payload, "entityIds")
        };
        await context.ModuleEntityService.SyncProjectLanguagesToTargetAsync(input);
        return LocalExecutionResult.Ok($"项目多语言同步完成，共处理 {input.EntityIds.Count} 个实体");
    }

    private async Task<LocalExecutionResult> SyncModuleToMenuAsync(LocalExecutionRequest request)
    {
        var moduleId = GetRequiredLong(request.Payload, "moduleId", "id");
        var module = await _serverClient.GetProjectModuleAsync(request.ServerBaseUrl, moduleId, request.AccessToken);
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, module.ProjectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        await context.ProjectModuleService.SyncModuleToMenuAsync(moduleId);
        return LocalExecutionResult.Ok("模块菜单同步完成");
    }

    private async Task<LocalExecutionResult> GetPageContentAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.GetPageContentAsync(entityId, pageType);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> SavePageContentAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var bundle = await GetBundleByEntityAsync(request, entityId);
        var input = new SavePageContentDto
        {
            TemplateHtml = GetString(request.Payload, "templateHtml"),
            TreeJson = GetString(request.Payload, "treeJson")
        };

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.SavePageContentAsync(entityId, pageType, input);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> GetPageScriptAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.GetPageScriptAsync(entityId, pageType);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> SavePageScriptAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var scriptJson = GetRequiredString(request.Payload, "scriptJson");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.SavePageScriptAsync(entityId, pageType, scriptJson);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> GetFieldScriptsAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.GetFieldScriptsAsync(entityId, pageType);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> SaveFieldScriptsAsync(LocalExecutionRequest request)
    {
        var entityId = GetRequiredLong(request.Payload, "entityId", "id");
        var pageType = GetRequiredString(request.Payload, "pageType");
        var scripts = GetRequiredObject<Dictionary<string, string>>(request.Payload, "scripts");
        var bundle = await GetBundleByEntityAsync(request, entityId);

        using var context = await _metadataStore.CreateAsync(bundle, GetFullProjectPathOverride(bundle.Project, request.Payload));
        var result = await context.ModuleEntityService.SaveFieldScriptsAsync(entityId, pageType, scripts);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> GetDictTypesAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        var project = bundle.Project;
        ApplyProjectOverrides(project, request.Payload);

        using var targetDb = CreateTargetDb(project);
        var dictTypes = await targetDb.Queryable<SysDictType>()
            .Where(t => t.Status == 0)
            .OrderBy(t => t.Sort)
            .ToListAsync();

        var result = dictTypes.Select(t => new DictTypeOptionDto
        {
            DictType = t.DictType,
            DictName = t.DictName
        }).ToList();

        return LocalExecutionResult.OkData(result);
    }

    private async Task<LocalExecutionResult> MigrateDatabaseAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var migratorPath = Path.Combine(projectPath, $"{project.ProjectName}.Migrator");
        var migrationName = $"Auto{DateTime.UtcNow:yyyyMMddHHmmss}";
        var output = new StringBuilder();

        output.AppendLine($"[1/3] dotnet build {project.ProjectName}.Migrator");
        output.AppendLine(await RunCommandAsync("dotnet", new[] { "build" }, migratorPath, TimeSpan.FromMinutes(3)));
        output.AppendLine($"[2/3] dotnet ef migrations add {migrationName}");
        output.AppendLine(await RunCommandAsync("dotnet", new[] { "ef", "migrations", "add", migrationName, "--no-build" }, migratorPath, TimeSpan.FromMinutes(2)));
        output.AppendLine("[3/3] dotnet run");
        output.AppendLine(await RunCommandAsync("dotnet", new[] { "run" }, migratorPath, TimeSpan.FromMinutes(5)));

        return LocalExecutionResult.Ok($"数据库迁移完成：{migrationName}", output: output.ToString());
    }

    private async Task<LocalExecutionResult> BuildProjectAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var slnPath = Path.Combine(projectPath, $"{project.ProjectName}.sln");
        var frontendPath = Path.Combine(projectPath, $"{project.ProjectName}.Vue");
        var output = new StringBuilder();
        try
        {
            output.AppendLine("[1/2] 构建后端解决方案...");
            output.AppendLine(await RunCommandAsync(
                "dotnet",
                new[] { "build", slnPath },
                projectPath,
                TimeSpan.FromMinutes(5)));

            if (Directory.Exists(frontendPath) && File.Exists(Path.Combine(frontendPath, "package.json")))
            {
                output.AppendLine("[2/2] 构建前端项目...");
                var npmCommand = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
                output.AppendLine(await RunCommandAsync(
                    npmCommand,
                    new[] { "run", "build" },
                    frontendPath,
                    TimeSpan.FromMinutes(10)));
            }
            else
            {
                output.AppendLine("[2/2] 未发现前端 package.json，跳过前端构建。");
            }

            return LocalExecutionResult.Ok("项目前后端构建完成", output: output.ToString());
        }
        catch (Exception ex)
        {
            var failureMessage = GetFirstLine(FormatExceptionMessage(ex));
            output.AppendLine(ex.ToString());
            return LocalExecutionResult.Fail(
                $"项目构建失败: {failureMessage}",
                BuildDiagnosticFormatter.Summarize(output.ToString(), failureMessage));
        }
    }

    private async Task<LocalExecutionResult> BuildFrontendAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var frontendPath = Path.Combine(projectPath, $"{project.ProjectName}.Vue");
        var npmCommand = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
        var output = await RunCommandAsync(
            npmCommand,
            new[] { "run", "build" },
            frontendPath,
            TimeSpan.FromMinutes(10));
        return LocalExecutionResult.Ok("Frontend build completed", output: output);
    }

    private async Task<LocalExecutionResult> EnhanceUiPageAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);
        var input = request.Payload.Deserialize<ProjectUiEnhancementDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        }) ?? throw new InvalidOperationException("Invalid UI enhancement payload");
        input.ProjectId = projectId;
        var projectPath = GetFullProjectPath(bundle.Project.ProjectName, bundle.Project.ProjectPath);

        ProjectUiEnhancementResultDto result;
        if (string.Equals(input.TargetKind, "EntityPage", StringComparison.OrdinalIgnoreCase))
        {
            if (!input.EntityId.HasValue)
                throw new InvalidOperationException("EntityPage design requires entityId");
            var entity = bundle.Entities.FirstOrDefault(item => item.Id == input.EntityId.Value)
                ?? throw new InvalidOperationException($"Entity not found: {input.EntityId.Value}");
            var module = bundle.Modules.FirstOrDefault(item => item.Id == entity.ModuleId)
                ?? throw new InvalidOperationException($"Entity module not found: {entity.ModuleId}");
            result = await ProjectUiDesignService.ApplyEntityPageAsync(
                bundle.Project,
                module,
                entity,
                input,
                projectPath);
        }
        else
        {
            result = await ProjectUiPageRenderer.ApplyAsync(
                bundle.Project,
                bundle.Modules,
                bundle.Entities,
                input,
                projectPath);
        }

        return LocalExecutionResult.Ok(result.Message, result);
    }

    private async Task<LocalExecutionResult> StartFrontendAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        return ToLocalExecutionResult(await ProjectProcessLauncher.StartFrontendAsync(project.ProjectName, projectPath, project.FrontendPort));
    }

    private async Task<LocalExecutionResult> StartBackendAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        return ToLocalExecutionResult(await ProjectProcessLauncher.StartBackendAsync(project.ProjectName, projectPath, project.BackendPort));
    }

    private async Task<LocalExecutionResult> StartProjectAsync(LocalExecutionRequest request)
    {
        await StartBackendAsync(request);
        await StartFrontendAsync(request);
        return LocalExecutionResult.Ok("前后端服务已启动");
    }

    private async Task<LocalExecutionResult> StopProjectAsync(LocalExecutionRequest request)
    {
        var backend = await StopProcessAsync(request, "backend");
        var frontend = await StopProcessAsync(request, "frontend");
        return LocalExecutionResult.Ok("前后端停止完成", new { backend, frontend });
    }

    private async Task<LocalExecutionResult> StopProcessAsync(LocalExecutionRequest request, string kind)
    {
        var project = await GetProjectAsync(request);
        var port = kind == "frontend" ? project.FrontendPort : project.BackendPort;
        if (ProjectProcessLauncher.TryGetListeningProcessIdByPort(port) is { } pid)
        {
            var portProcess = Process.GetProcessById(pid);
            portProcess.Kill(entireProcessTree: true);
            portProcess.Dispose();
            return LocalExecutionResult.Ok($"{kind} process stopped (PID {pid})");
        }

        return LocalExecutionResult.Fail($"{kind} process is not running or not tracked by LocalAgent");
    }

    private async Task<LocalExecutionResult> GetProjectStatusAsync(LocalExecutionRequest request)
    {
        var project = await GetProjectAsync(request);
        var backendPid = ProjectProcessLauncher.TryGetListeningProcessIdByPort(project.BackendPort);
        var frontendPid = ProjectProcessLauncher.TryGetListeningProcessIdByPort(project.FrontendPort);

        return LocalExecutionResult.OkData(new ProjectStatusDto
        {
            BackendRunning = backendPid.HasValue || ProjectProcessLauncher.IsPortListening(project.BackendPort),
            FrontendRunning = frontendPid.HasValue || ProjectProcessLauncher.IsPortListening(project.FrontendPort),
            BackendPid = backendPid,
            FrontendPid = frontendPid
        });
    }

    private async Task<Project> GetProjectAsync(LocalExecutionRequest request)
    {
        var projectId = GetRequiredLong(request.Payload, "projectId", "id");
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, projectId, request.AccessToken);
        var project = bundle.Project;
        ApplyProjectOverrides(project, request.Payload);
        return project;
    }

    private async Task<GenerationBundleDto> GetBundleByEntityAsync(LocalExecutionRequest request, long entityId)
    {
        var entity = await _serverClient.GetModuleEntityAsync(request.ServerBaseUrl, entityId, request.AccessToken);
        var bundle = await _serverClient.GetGenerationBundleAsync(request.ServerBaseUrl, entity.ProjectId, request.AccessToken);
        ApplyProjectOverrides(bundle.Project, request.Payload);
        return bundle;
    }

    private async Task<LocalExecutionResult> DownloadTemplateAsync(LocalExecutionRequest request)
    {
        var template = await _serverClient.DownloadTemplateAsync(request.ServerBaseUrl, request.AccessToken);
        var zipPath = await SaveTemplateZipBytesAsync(template.FileName, template.Content);

        return LocalExecutionResult.Ok("模板已保存到本地", new
        {
            fileName = Path.GetFileName(zipPath),
            filePath = zipPath
        });
    }

    private async Task<string> SaveTemplateZipAsync(long projectId, string templateBase64)
    {
        var fileName = $"CodeMaster_Template_{projectId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.zip";
        return await SaveTemplateZipBytesAsync(fileName, Convert.FromBase64String(templateBase64));
    }

    private async Task<string> SaveTemplateZipBytesAsync(string fileName, byte[] content)
    {
        if (content.Length == 0)
            throw new InvalidOperationException("Template file cannot be empty");

        var templateDir = _metadataStore.GetTemplatesDirectory();
        Directory.CreateDirectory(templateDir);

        var zipPath = Path.Combine(templateDir, NormalizeTemplateZipFileName(fileName));
        await File.WriteAllBytesAsync(zipPath, content);
        File.SetLastWriteTimeUtc(zipPath, DateTime.UtcNow);
        return zipPath;
    }

    private static string NormalizeTemplateZipFileName(string? fileName)
    {
        var safeFileName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName) ||
            !safeFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            !safeFileName.StartsWith("CodeMaster_Template_", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName = $"CodeMaster_Template_{DateTime.UtcNow:yyyyMMddHHmmssfff}.zip";
        }

        return safeFileName;
    }

    private string? FindLocalTemplateZipPath()
    {
        var searchRoots = new[]
        {
            Path.GetDirectoryName(Environment.ProcessPath),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            _metadataStore.GetMetadataRoot()
        };

        return searchRoots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .SelectMany(path => GetTemplateZipDirectoryCandidates(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "CodeMaster_Template_*.zip"))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static IEnumerable<string> GetTemplateZipDirectoryCandidates(string root)
    {
        yield return Path.Combine(root, "Templates");
        yield return Path.Combine(root, "templates");
        yield return Path.Combine(root, "resources", "Templates");
    }

    private SqlSugarClient CreateTargetDb(Project project)
    {
        var dbType = ToSqlSugarDbType(project.DatabaseType);
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = NormalizeConnectionString(project),
            DbType = dbType,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(dbType)
        });
    }

    private static DbType ToSqlSugarDbType(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.MySQL => DbType.MySql,
            DatabaseType.SqlServer => DbType.SqlServer,
            DatabaseType.PostgreSQL => DbType.PostgreSQL,
            DatabaseType.SQLite => DbType.Sqlite,
            DatabaseType.Oracle => DbType.Oracle,
            _ => DbType.SqlServer
        };
    }

    private static string NormalizeConnectionString(Project project)
    {
        var connectionString = project.ConnectionString ?? string.Empty;
        if (project.DatabaseType != DatabaseType.SQLite)
            return connectionString;

        var match = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return connectionString;

        var dataSource = match.Groups[1].Value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dataSource) || Path.IsPathRooted(dataSource))
            return connectionString;

        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var absolutePath = Path.GetFullPath(Path.Combine(projectPath, $"{project.ProjectName}.Migrator", dataSource));
        return Regex.Replace(connectionString, @"Data Source\s*=\s*([^;]+)", "Data Source=" + absolutePath, RegexOptions.IgnoreCase);
    }

    private static async Task<string> RunCommandAsync(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
    {
        if (!Directory.Exists(workingDirectory))
            throw new DirectoryNotFoundException(workingDirectory);

        using var cts = new CancellationTokenSource(timeout);
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        DotnetProcessEnvironment.Apply(startInfo);
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process: {fileName}");

        process.StandardInput.Close();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            await process.WaitForExitAsync();
            var timedOutOutput = CombineProcessOutput(
                await standardOutputTask,
                await standardErrorTask);
            throw new TimeoutException(
                $"{fileName} {string.Join(' ', args)} timed out after {timeout.TotalMinutes:0.#} minutes.\n{timedOutOutput}",
                ex);
        }

        var output = CombineProcessOutput(
            await standardOutputTask,
            await standardErrorTask);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} {string.Join(' ', args)} exited with code {process.ExitCode}\n{output}");

        return output;
    }

    private static string CombineProcessOutput(string standardOutput, string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardOutput))
            return standardError;
        if (string.IsNullOrWhiteSpace(standardError))
            return standardOutput;

        return standardOutput.TrimEnd() + Environment.NewLine + standardError;
    }


    private async Task TryCompleteServerMetadataAsync(string action, LocalExecutionRequest request, LocalExecutionResult result)
    {
        if (!ShouldCompleteServerMetadata(action))
            return;

        var complete = new LocalExecutionCompleteDto
        {
            Action = action,
            ProjectId = GetOptionalLong(request.Payload, "projectId"),
            EntityId = GetOptionalLong(request.Payload, "entityId"),
            ModuleId = GetOptionalLong(request.Payload, "moduleId"),
            CompletedAt = DateTime.UtcNow
        };

        if (action is "generateProjectCode" or "generateProjectIncrementalCode")
        {
            complete.EntityIds = GetLongList(request.Payload, "entityIds");
            if (result.Data != null)
            {
                var resultData = JsonSerializer.SerializeToElement(result.Data);
                if (TryGetProperty(resultData, "entityIds", out _))
                {
                    complete.EntityIds = GetLongList(resultData, "entityIds");
                    if (complete.EntityIds.Count == 0)
                        return;
                }
            }
        }

        if (!complete.ProjectId.HasValue &&
            action is "initializeProject" or "initializeStep11" or "startProject" or "stopProject")
        {
            complete.ProjectId = GetOptionalLong(request.Payload, "id");
        }

        if (!complete.EntityId.HasValue && action is "generateCode" or "generateIncrementalCode")
        {
            complete.EntityId = GetOptionalLong(request.Payload, "id");
        }

        if (!complete.ModuleId.HasValue && action == "syncModuleToMenu")
        {
            complete.ModuleId = GetOptionalLong(request.Payload, "id");
        }

        try
        {
            await _serverClient.CompleteLocalExecutionAsync(request.ServerBaseUrl, complete, request.AccessToken);
        }
        catch (Exception ex)
        {
            result.Message = $"{result.Message}；服务器状态回写失败：{ex.Message}";
        }
    }

    private static bool ShouldCompleteServerMetadata(string action)
    {
        return action is
            "initializeProject" or
            "initializeStep11" or
            "startProject" or
            "stopProject" or
            "generateCode" or
            "generateIncrementalCode" or
            "generateProjectCode" or
            "generateProjectIncrementalCode" or
            "syncModuleToMenu";
    }


    private static LocalExecutionResult StepOk(string message, string step, int progress, string? output = null)
    {
        return LocalExecutionResult.Ok(message, new InitializeStepResultDto
        {
            Success = true,
            Message = message,
            Step = step,
            Progress = progress
        }, output);
    }

    private static LocalExecutionResult ToLocalExecutionResult(ProjectActionResultDto result)
    {
        return result.Success
            ? LocalExecutionResult.Ok(result.Message, result, result.Output)
            : LocalExecutionResult.Fail(result.Message, result.Output);
    }

    private static void ApplyProjectOverrides(Project project, JsonElement payload)
    {
        var projectPathOverride = GetProjectPathOverride(payload);
        if (!string.IsNullOrWhiteSpace(projectPathOverride))
            project.ProjectPath = GetFullProjectPath(project.ProjectName, projectPathOverride);

        var connectionStringOverride = GetString(payload, "connectionString");
        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
            project.ConnectionString = connectionStringOverride;

        var databaseTypeOverride = GetString(payload, "databaseType");
        if (!string.IsNullOrWhiteSpace(databaseTypeOverride) &&
            TryReadDatabaseType(databaseTypeOverride, out var databaseType))
        {
            project.DatabaseType = databaseType;
        }
    }

    private static string? GetProjectPathOverride(JsonElement payload)
    {
        return GetString(payload, "projectPath") ?? GetString(payload, "targetPath");
    }

    private static string? GetFullProjectPathOverride(Project project, JsonElement payload)
    {
        var overridePath = GetProjectPathOverride(payload);
        return string.IsNullOrWhiteSpace(overridePath)
            ? null
            : GetFullProjectPath(project.ProjectName, overridePath);
    }

    private static string GetFullProjectPath(string projectName, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new InvalidOperationException("Project path is required");

        return targetPath.EndsWith(projectName, StringComparison.OrdinalIgnoreCase)
            ? targetPath
            : Path.Combine(targetPath, projectName);
    }

    private static long GetRequiredLong(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(payload, name, out var value) && TryReadLong(value, out var result))
                return result;
        }

        throw new InvalidOperationException($"Missing required payload value: {string.Join("/", names)}");
    }

    private static long? GetOptionalLong(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(payload, name, out var value) && TryReadLong(value, out var result))
                return result;
        }

        return null;
    }

    private static List<long> GetLongList(JsonElement payload, string name)
    {
        if (!TryGetProperty(payload, name, out var value) || value.ValueKind != JsonValueKind.Array)
            return new List<long>();

        var result = new List<long>();
        foreach (var item in value.EnumerateArray())
        {
            if (TryReadLong(item, out var id) && id > 0)
                result.Add(id);
        }

        return result.Distinct().ToList();
    }

    private static string GetRequiredString(JsonElement payload, string name)
    {
        var value = GetString(payload, name);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        throw new InvalidOperationException($"Missing required payload value: {name}");
    }

    private static string? GetString(JsonElement payload, string name)
    {
        if (!TryGetProperty(payload, name, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static T GetRequiredObject<T>(JsonElement payload, string name)
    {
        if (!TryGetProperty(payload, name, out var value))
            throw new InvalidOperationException($"Missing required payload value: {name}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = value.ValueKind == JsonValueKind.String
            ? JsonSerializer.Deserialize<T>(value.GetString() ?? string.Empty, options)
            : value.Deserialize<T>(options);

        return result ?? throw new InvalidOperationException($"Invalid payload value: {name}");
    }

    private static bool TryGetProperty(JsonElement payload, string name, out JsonElement value)
    {
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out value))
            return true;

        value = default;
        return false;
    }

    private static bool TryReadLong(JsonElement value, out long result)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out result))
            return true;

        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out result))
            return true;

        result = default;
        return false;
    }

    private static bool TryReadDatabaseType(string value, out DatabaseType result)
    {
        if (Enum.TryParse(value, ignoreCase: true, out result))
            return true;

        if (int.TryParse(value, out var number) && Enum.IsDefined(typeof(DatabaseType), number))
        {
            result = (DatabaseType)number;
            return true;
        }

        result = default;
        return false;
    }

    private static string FormatExceptionMessage(Exception ex)
    {
        var baseException = ex.GetBaseException();
        var message = baseException.Message;

        if (string.IsNullOrWhiteSpace(message))
            message = ex.Message;

        if (string.IsNullOrWhiteSpace(message))
            message = baseException.GetType().FullName ?? ex.GetType().FullName ?? "Unknown local execution error";

        return message;
    }

    private static string GetFirstLine(string value)
    {
        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .FirstOrDefault()
            ?.Trim() ?? string.Empty;
    }
}
