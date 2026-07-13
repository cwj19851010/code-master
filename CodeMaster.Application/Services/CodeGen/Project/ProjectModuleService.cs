using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using Mapster;
using SqlSugar;
using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 项目模块服务
/// </summary>
public class ProjectModuleService : CrudApplicationService<ProjectModule, ProjectModuleDto, ProjectModuleDto, PagedQueryDto, CreateProjectModuleDto, UpdateProjectModuleDto>, IProjectModuleService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly ISqlSugarClient _sqlSugarClient;

    public ProjectModuleService(
        IRepository<ProjectModule> repository,
        IExcelService excelService,
        IRepository<Project> projectRepository,
        ISqlSugarClient sqlSugarClient,
        CodeMaster.Core.Services.ICacheService? cacheService = null) : base(repository, excelService, cacheService)
    {
        _projectRepository = projectRepository;
        _sqlSugarClient = sqlSugarClient;
    }

    /// <summary>
    /// 根据项目ID获取模块列表
    /// </summary>
    public async Task<List<ProjectModuleDto>> GetByProjectIdAsync(long projectId)
    {
        var modules = await _sqlSugarClient.Queryable<ProjectModule>()
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.OrderNum)
            .ToListAsync();

        return modules.Adapt<List<ProjectModuleDto>>();
    }

    /// <summary>
    /// 同步模块到目标项目菜单（服务端模式）
    /// </summary>
    public async Task<bool> SyncModuleToMenuAsync(long moduleId)
    {
        // 1. 获取模块信息
        var module = await Repository.GetByIdAsync(moduleId);
        if (module == null)
            throw new Exception("模块不存在");
        var moduleNameLower = module.ModuleName.ToLower();
        var moduleTitleKey = ToCamelCase(module.ModuleName);
        // 2. 获取项目信息
        var project = await _projectRepository.GetByIdAsync(module.ProjectId);
        if (project == null)
            throw new Exception("项目不存在");

        // 3. 创建目标数据库连接（SQLite 相对路径自动转绝对路径）
        var targetConnectionString = NormalizeConnectionString(project);
        var dbType = GetDbType(project.DatabaseType);
        var targetDb = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = targetConnectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(dbType)
        });

        var path = moduleNameLower;
        // 4. 检查菜单是否已存在
        var existingMenu = await targetDb.Queryable<SysMenu>()
            .Where(m => m.Path == path)
            .FirstAsync();

        if (existingMenu != null)
        {
            // 更新菜单
            existingMenu.MenuName = module.ModuleDescription;
            existingMenu.TitleKey = moduleTitleKey;
            existingMenu.Icon = string.IsNullOrWhiteSpace(existingMenu.Icon) ? "Fold" : existingMenu.Icon;
            existingMenu.OrderNum = existingMenu.OrderNum;
            existingMenu.UpdateTime = DateTime.UtcNow;
            existingMenu.Component = "Layout";

            await targetDb.Updateable(existingMenu).ExecuteCommandAsync();
        }
        else
        {
            // 插入新菜单
            var newMenu = new SysMenu
            {
                Id = GenerateMenuId(),
                MenuName = module.ModuleDescription,
                TitleKey = moduleTitleKey,
                ParentId = null,
                OrderNum = module.OrderNum==0?99:module.OrderNum,
                Path = moduleNameLower,
                Component = "Layout",
                MenuType = "M",
                Visible = true,
                Status = 0,
                Icon = string.IsNullOrWhiteSpace(module.Icon) ? "Fold" : module.Icon,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(newMenu).ExecuteCommandAsync();
        }

        // 5. 同步国际化
        await SyncLanguageAsync(targetDb, moduleTitleKey, module.ModuleDescription);

        // 6. 更新模块同步状态
        module.IsSynced = true;
        module.LastSyncTime = DateTime.UtcNow;
        await Repository.UpdateAsync(module);

        return true;
    }

    /// <summary>
    /// 获取客户端同步数据
    /// </summary>
    public async Task<ClientSyncModuleToMenuDto> GetClientSyncDataAsync(long moduleId)
    {
        // 1. 获取模块信息
        var module = await Repository.GetByIdAsync(moduleId);
        if (module == null)
            throw new Exception("模块不存在");

        // 2. 获取项目信息
        var project = await _projectRepository.GetByIdAsync(module.ProjectId);
        if (project == null)
            throw new Exception("项目不存在");

        return new ClientSyncModuleToMenuDto
        {
            ModuleId = module.Id,
            ProjectId = project.Id,
            ProjectName = project.ProjectName,
            ProjectPath = project.ProjectPath,
            DatabaseType = project.DatabaseType.ToString(),
            ConnectionString = project.ConnectionString,
            ModuleName = module.ModuleName,
            ModuleDescription = module.ModuleDescription,
            Icon = module.Icon,
            OrderNum = module.OrderNum
        };
    }

    /// <summary>
    /// 同步国际化
    /// </summary>
    private async Task SyncLanguageAsync(ISqlSugarClient targetDb, string langKey, string description)
    {
        // 检查中文翻译是否存在
        var existingZh = await targetDb.Queryable<SysLangText>()
            .Where(t => t.LangKey == langKey && t.LangCode == "zh-CN")
            .FirstAsync();

        if (existingZh != null)
        {
            // 更新中文翻译
            existingZh.LangValue = description;
            existingZh.UpdateTime = DateTime.UtcNow;
            await targetDb.Updateable(existingZh).ExecuteCommandAsync();
        }
        else
        {
            // 插入中文翻译
            var newZh = new SysLangText
            {
                Id = GenerateId(),
                LangKey = langKey,
                LangCode = "zh-CN",
                LangValue = description,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(newZh).ExecuteCommandAsync();
        }

        // 检查英文翻译是否存在
        var existingEn = await targetDb.Queryable<SysLangText>()
            .Where(t => t.LangKey == langKey && t.LangCode == "en-US")
            .FirstAsync();

        if (existingEn != null)
        {
            // 更新英文翻译
            existingEn.LangValue = langKey;
            existingEn.UpdateTime = DateTime.UtcNow;
            await targetDb.Updateable(existingEn).ExecuteCommandAsync();
        }
        else
        {
            // 插入英文翻译
            var newEn = new SysLangText
            {
                Id = GenerateId(),
                LangKey = langKey,
                LangCode = "en-US",
                LangValue = langKey,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(newEn).ExecuteCommandAsync();
        }
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// 规范化连接字符串（SQLite 相对路径 -> 基于项目路径的绝对路径）
    /// </summary>
    private string NormalizeConnectionString(Project project)
    {
        if (project.DatabaseType != Core.Enums.DatabaseType.SQLite)
        {
            return project.ConnectionString;
        }

        var connectionString = project.ConnectionString ?? string.Empty;
        var match = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return connectionString;
        }

        var dataSource = match.Groups[1].Value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dataSource) || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        var projectPath = ResolveProjectRootPath(project);
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return connectionString;
        }

        // 目标项目数据库通常位于 {ProjectPath}/{ProjectName}.Migrator 下
        var absolutePath = Path.GetFullPath(Path.Combine(projectPath, $"{project.ProjectName}.Migrator", dataSource));
        return Regex.Replace(
            connectionString,
            @"Data Source\s*=\s*([^;]+)",
            $"Data Source={absolutePath}",
            RegexOptions.IgnoreCase);
    }

    private static string ResolveProjectRootPath(Project project)
    {
        var projectPath = project.ProjectPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return projectPath;
        }

        return projectPath.EndsWith(project.ProjectName, StringComparison.OrdinalIgnoreCase)
            ? projectPath
            : Path.Combine(projectPath, project.ProjectName);
    }

    /// <summary>
    /// 获取数据库类型
    /// </summary>
    private DbType GetDbType(Core.Enums.DatabaseType databaseType)
    {
        return databaseType switch
        {
            Core.Enums.DatabaseType.MySQL => DbType.MySql,
            Core.Enums.DatabaseType.SqlServer => DbType.SqlServer,
            Core.Enums.DatabaseType.PostgreSQL => DbType.PostgreSQL,
            Core.Enums.DatabaseType.SQLite => DbType.Sqlite,
            Core.Enums.DatabaseType.Oracle => DbType.Oracle,
            _ => throw new NotSupportedException($"不支持的数据库类型: {databaseType}")
        };
    }

    /// <summary>
    /// 生成菜单ID（雪花ID）
    /// </summary>
    private long GenerateMenuId()
    {
        return DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// 生成ID（雪花ID）
    /// </summary>
    private long GenerateId()
    {
        return DateTime.UtcNow.Ticks;
    }
}
