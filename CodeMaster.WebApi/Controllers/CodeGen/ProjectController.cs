using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Common;
using CodeMaster.Core.Dtos;

namespace CodeMaster.WebApi.Controllers.CodeGen;

/// <summary>
/// 项目管理控制器
/// </summary>
[ApiController]
[Route("api/codegen/project")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// 分页查询项目
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPagedList([FromQuery] ProjectQueryDto query)
    {
        try
        {
            var result = await _projectService.GetPagedListAsync(query);
            return Ok(ApiResponse<PagedResultDto<ProjectDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PagedResultDto<ProjectDto>>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取项目
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var result = await _projectService.GetByIdAsync(id);
            if (result == null)
                return NotFound(ApiResponse<ProjectDto>.Fail("项目不存在"));

            return Ok(ApiResponse<ProjectDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        try
        {
            var id = await _projectService.CreateAsync(dto);
            return Ok(ApiResponse<long>.Success(id, "创建成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<long>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectDto dto)
    {
        try
        {
            dto.Id = id;
            var result = await _projectService.UpdateAsync(id, dto);
            return Ok(ApiResponse<int>.Success(result, "更新成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<int>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var result = await _projectService.DeleteAsync(id);
            return Ok(ApiResponse<int>.Success(result, "删除成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<int>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 导出模板
    /// </summary>
    [HttpPost("export-template")]
    public async Task<IActionResult> ExportTemplate([FromBody] ExportTemplateDto dto)
    {
        try
        {
            if (!IsHostAdminUser())
                return HostAdminOnly();

            var result = await _projectService.ExportTemplateAsync(dto.OutputPath);
            return Ok(ApiResponse<string>.Success(result, "导出成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取模板 Base64（用于客户端）
    /// </summary>
    [HttpGet("template-base64")]
    public async Task<IActionResult> GetTemplateBase64()
    {
        try
        {
            var result = await _projectService.GetTemplateBase64Async();
            return Ok(ApiResponse<string>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取客户端初始化数据
    /// </summary>
    /// <summary>
    /// Download latest project template ZIP from the server.
    /// </summary>
    [HttpGet("template/download")]
    public async Task<IActionResult> DownloadTemplate()
    {
        try
        {
            var result = await _projectService.GetTemplateFileAsync();
            return File(result.Content, "application/zip", result.FileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Upload a project template ZIP. Only host Admin can replace the server template.
    /// </summary>
    [HttpPost("template/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(500L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 500L * 1024 * 1024)]
    public async Task<IActionResult> UploadTemplate(IFormFile file)
    {
        try
        {
            if (!IsHostAdminUser())
                return HostAdminOnly();

            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<string>.Fail("Template file cannot be empty"));

            await using var stream = file.OpenReadStream();
            var result = await _projectService.SaveTemplateFileAsync(stream, file.FileName, file.Length);
            return Ok(ApiResponse<ProjectTemplateUploadResultDto>.Success(result, "Template uploaded"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProjectTemplateUploadResultDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id}/client-init-data")]
    public async Task<IActionResult> GetClientInitData(long id)
    {
        try
        {
            var result = await _projectService.GetClientInitializeDataAsync(id);
            return Ok(ApiResponse<ClientInitializeProjectDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ClientInitializeProjectDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取客户端本地执行代码生成所需的完整上下文快照。
    /// </summary>
    [HttpGet("{id}/generation-bundle")]
    public async Task<IActionResult> GetGenerationBundle(long id)
    {
        try
        {
            var result = await _projectService.GetGenerationBundleAsync(id);
            return Ok(ApiResponse<GenerationBundleDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<GenerationBundleDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 客户端本地执行完成后回写服务器元数据状态。
    /// </summary>
    [HttpPost("local-execution/completed")]
    public async Task<IActionResult> CompleteLocalExecution([FromBody] LocalExecutionCompleteDto dto)
    {
        try
        {
            var result = await _projectService.CompleteLocalExecutionAsync(dto);
            return Ok(ApiResponse<bool>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    #region 分步初始化 API

    /// <summary>
    /// 步骤1：解压模板并替换项目名称
    /// </summary>
    [HttpPost("initialize/step1")]
    public async Task<IActionResult> InitializeStep1([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step1_ExtractTemplateAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤2：生成解决方案文件
    /// </summary>
    [HttpPost("initialize/step2")]
    public async Task<IActionResult> InitializeStep2([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step2_GenerateSolutionAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤3：更新数据库配置
    /// </summary>
    [HttpPost("initialize/step3")]
    public async Task<IActionResult> InitializeStep3([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step3_UpdateDatabaseConfigAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤4：更新端口配置
    /// </summary>
    [HttpPost("initialize/step4")]
    public async Task<IActionResult> InitializeStep4([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step4_UpdatePortConfigAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤5：创建数据库迁移
    /// </summary>
    [HttpPost("initialize/step5")]
    public async Task<IActionResult> InitializeStep5([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step5_CreateMigrationAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤6：应用数据库迁移
    /// </summary>
    [HttpPost("initialize/step6")]
    public async Task<IActionResult> InitializeStep6([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step6_ApplyMigrationAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤7：运行 dotnet restore
    /// </summary>
    [HttpPost("initialize/step7")]
    public async Task<IActionResult> InitializeStep7([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step7_DotnetRestoreAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤8：写入项目翻译
    /// </summary>
    [HttpPost("initialize/step8")]
    public async Task<IActionResult> InitializeStep8([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step8_WriteTranslationsAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤9：运行 npm install
    /// </summary>
    [HttpPost("initialize/step9")]
    public async Task<IActionResult> InitializeStep9([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step9_NpmInstallAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤10：启动后端服务
    /// </summary>
    [HttpPost("initialize/step10")]
    public async Task<IActionResult> InitializeStep10([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step10_StartBackendAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 步骤11：启动前端服务
    /// </summary>
    [HttpPost("initialize/step11")]
    public async Task<IActionResult> InitializeStep11([FromBody] InitializeStepDto dto)
    {
        try
        {
            var result = await _projectService.Step11_StartFrontendAsync(dto);
            return Ok(ApiResponse<InitializeStepResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializeStepResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取初始化状态
    /// </summary>
    [HttpGet("{id}/initialization-state")]
    public async Task<IActionResult> GetInitializationState(long id)
    {
        try
        {
            var result = await _projectService.GetInitializationStateAsync(id);
            return Ok(ApiResponse<InitializationStateDto>.Success(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InitializationStateDto>.Fail(ex.Message));
        }
    }

    #endregion

    /// <summary>
    /// 初始化项目（旧版，一次性完成）
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializeProjectDto dto)
    {
        try
        {
            var result = await _projectService.InitializeAsync(dto);
            return Ok(ApiResponse<bool>.Success(result, "初始化成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 启动项目
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(long id)
    {
        try
        {
            var result = await _projectService.StartAsync(id);
            return Ok(ApiResponse<bool>.Success(result, "启动成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 停止项目
    /// </summary>
    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(long id)
    {
        try
        {
            var result = await _projectService.StopAsync(id);
            return Ok(ApiResponse<bool>.Success(result, "停止成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    private bool IsHostAdminUser()
    {
        var isHostAdminClaim = User.FindFirst("IsHostAdmin")?.Value;
        if (bool.TryParse(isHostAdminClaim, out var isHostAdmin) && isHostAdmin)
            return true;

        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        var isHostTenant = long.TryParse(tenantIdClaim, out var tenantId) && tenantId == 0;
        var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;

        return isHostTenant
            && !string.IsNullOrWhiteSpace(userName)
            && userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }

    private ObjectResult HostAdminOnly()
    {
        return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Only host Admin can manage project templates"));
    }
}
