using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// SysMenu服务接口
/// </summary>
public interface ISysMenuService : ICrudService<SysMenuDto, CreateSysMenuDto, UpdateSysMenuDto, SysMenuQueryDto>
{
}
