using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 项目模块服务接口
/// </summary>
public interface IProjectModuleService : ICrudApplicationService<ProjectModule, ProjectModuleDto, ProjectModuleDto, PagedQueryDto, CreateProjectModuleDto, UpdateProjectModuleDto>, IApplicationService
{
    /// <summary>
    /// 根据项目ID获取模块列表
    /// </summary>
    Task<List<ProjectModuleDto>> GetByProjectIdAsync(long projectId);

    /// <summary>
    /// 获取全部模块列表
    /// </summary>
    Task<List<ProjectModuleDto>> GetListAsync(PagedQueryDto query);

    /// <summary>
    /// 同步模块到目标项目菜单（服务端模式）
    /// </summary>
    Task<bool> SyncModuleToMenuAsync(long moduleId);

    /// <summary>
    /// 获取客户端同步数据
    /// </summary>
    Task<ClientSyncModuleToMenuDto> GetClientSyncDataAsync(long moduleId);
}
