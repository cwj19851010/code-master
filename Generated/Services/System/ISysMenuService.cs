using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.System;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysMenu服务接口
/// </summary>
public interface ISysMenuService : ICrudService<SysMenuDto, CreateSysMenuDto, UpdateSysMenuDto, SysMenuQueryDto>
{
}
