using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// SysTenant服务接口
/// </summary>
public interface ISysTenantService : ICrudService<SysTenantDto, CreateSysTenantDto, UpdateSysTenantDto, SysTenantQueryDto>
{
}
