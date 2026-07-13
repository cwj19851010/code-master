using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.System;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysRole服务接口
/// </summary>
public interface ISysRoleService : ICrudService<SysRoleDto, CreateSysRoleDto, UpdateSysRoleDto, SysRoleQueryDto>
{
}
