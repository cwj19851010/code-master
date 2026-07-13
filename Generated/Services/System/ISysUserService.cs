using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.System;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysUser服务接口
/// </summary>
public interface ISysUserService : ICrudService<SysUserDto, CreateSysUserDto, UpdateSysUserDto, SysUserQueryDto>
{
}
