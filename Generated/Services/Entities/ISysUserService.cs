using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// SysUser服务接口
/// </summary>
public interface ISysUserService : ICrudService<SysUserDto, CreateSysUserDto, UpdateSysUserDto, SysUserQueryDto>
{
}
