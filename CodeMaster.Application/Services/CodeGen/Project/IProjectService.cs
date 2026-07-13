using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 项目管理服务接口
/// </summary>
public interface IProjectService : IApplicationService
{
    /// <summary>
    /// 导出模板
    /// </summary>
    Task<string> ExportTemplateAsync(string outputPath);

    /// <summary>
    /// 获取模板 Base64（用于客户端）
    /// </summary>
    Task<string> GetTemplateBase64Async();

    /// <summary>
    /// Get the latest project template ZIP file for download.
    /// </summary>
    Task<ProjectTemplateFileDto> GetTemplateFileAsync();

    /// <summary>
    /// Save a new project template ZIP file.
    /// </summary>
    Task<ProjectTemplateUploadResultDto> SaveTemplateFileAsync(Stream stream, string fileName, long length);

    /// <summary>
    /// 获取客户端初始化数据（用于客户端 WebView）
    /// </summary>
    Task<ClientInitializeProjectDto> GetClientInitializeDataAsync(long id);

    /// <summary>
    /// 获取客户端本地执行代码生成所需的完整上下文快照。
    /// </summary>
    Task<GenerationBundleDto> GetGenerationBundleAsync(long id);

    /// <summary>
    /// 客户端本地执行完成后回写服务器元数据状态。
    /// </summary>
    Task<bool> CompleteLocalExecutionAsync(LocalExecutionCompleteDto input);

    /// <summary>
    /// 初始化项目（旧版，一次性完成）
    /// </summary>
    Task<bool> InitializeAsync(InitializeProjectDto input);

    // ========== 分步初始化 API ==========

    /// <summary>
    /// 步骤1：解压模板并替换项目名称
    /// </summary>
    Task<InitializeStepResultDto> Step1_ExtractTemplateAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤2：生成解决方案文件
    /// </summary>
    Task<InitializeStepResultDto> Step2_GenerateSolutionAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤3：更新数据库配置
    /// </summary>
    Task<InitializeStepResultDto> Step3_UpdateDatabaseConfigAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤4：更新端口配置
    /// </summary>
    Task<InitializeStepResultDto> Step4_UpdatePortConfigAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤5：创建数据库迁移
    /// </summary>
    Task<InitializeStepResultDto> Step5_CreateMigrationAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤6：应用数据库迁移
    /// </summary>
    Task<InitializeStepResultDto> Step6_ApplyMigrationAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤7：运行 dotnet restore
    /// </summary>
    Task<InitializeStepResultDto> Step7_DotnetRestoreAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤8：写入项目翻译
    /// </summary>
    Task<InitializeStepResultDto> Step8_WriteTranslationsAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤9：运行 npm install
    /// </summary>
    Task<InitializeStepResultDto> Step9_NpmInstallAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤10：启动后端服务
    /// </summary>
    Task<InitializeStepResultDto> Step10_StartBackendAsync(InitializeStepDto input);

    /// <summary>
    /// 步骤11：启动前端服务
    /// </summary>
    Task<InitializeStepResultDto> Step11_StartFrontendAsync(InitializeStepDto input);

    /// <summary>
    /// 获取初始化状态
    /// </summary>
    Task<InitializationStateDto> GetInitializationStateAsync(long projectId);

    // ========== 项目管理 ==========

    /// <summary>
    /// 启动项目
    /// </summary>
    Task<bool> StartAsync(long id);

    /// <summary>
    /// 停止项目
    /// </summary>
    Task<bool> StopAsync(long id);

    /// <summary>
    /// 独立启动前端（npm run dev）
    /// </summary>
    Task<ProjectActionResultDto> StartFrontendAsync(ProjectActionDto input);

    /// <summary>
    /// 独立启动后端（dotnet run）
    /// </summary>
    Task<ProjectActionResultDto> StartBackendAsync(ProjectActionDto input);

    /// <summary>
    /// 独立停止前端进程
    /// </summary>
    Task<ProjectActionResultDto> StopFrontendAsync(ProjectActionDto input);

    /// <summary>
    /// 独立停止后端进程
    /// </summary>
    Task<ProjectActionResultDto> StopBackendAsync(ProjectActionDto input);

    /// <summary>
    /// 查询前后端运行状态
    /// </summary>
    Task<ProjectStatusDto> GetStatusAsync(ProjectActionDto input);

    /// <summary>
    /// 执行数据库迁移（EF Core add-migration + update-database）
    /// 迁移名称格式：Migration_{yyyyMMddHHmmss}
    /// </summary>
    Task<ProjectActionResultDto> MigrateDatabaseAsync(ProjectActionDto input);

    /// <summary>
    /// 编译目标项目（dotnet build）
    /// </summary>
    Task<ProjectActionResultDto> BuildAsync(ProjectActionDto input);

    /// <summary>
    /// 获取目标项目的字典类型列表
    /// </summary>
    Task<List<DictTypeOptionDto>> GetDictTypesAsync(long projectId);

    /// <summary>
    /// 根据ID获取项目
    /// </summary>
    Task<ProjectDto?> GetByIdAsync(long id);

    /// <summary>
    /// 获取全部项目列表
    /// </summary>
    Task<List<ProjectDto>> GetListAsync(ProjectQueryDto query);

    /// <summary>
    /// 分页查询项目
    /// </summary>
    Task<PagedResultDto<ProjectDto>> GetPagedListAsync(ProjectQueryDto query);

    /// <summary>
    /// 创建项目
    /// </summary>
    Task<long> CreateAsync(CreateProjectDto dto);

    /// <summary>
    /// 更新项目
    /// </summary>
    Task<int> UpdateAsync(long id, UpdateProjectDto dto);

    /// <summary>
    /// 删除项目
    /// </summary>
    Task<int> DeleteAsync(long id);
}
