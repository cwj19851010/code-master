using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.System;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysTenant服务接口
/// </summary>
public interface ISysTenantService : ICrudService<SysTenantDto, CreateSysTenantDto, UpdateSysTenantDto, SysTenantQueryDto>
{
}
