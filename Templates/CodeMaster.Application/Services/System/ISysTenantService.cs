using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 租户管理服务接口
/// </summary>
public interface ISysTenantService : IApplicationService
{
    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    Task<SysTenantDto?> GetByIdAsync(long id);

    /// <summary>
    /// 分页查询租户
    /// </summary>
    Task<PagedResultDto<SysTenantDto>> GetPagedListAsync([FromQuery] SysTenantQueryDto query);

    /// <summary>
    /// 创建租户
    /// </summary>
    Task<SysTenantDto> CreateAsync(CreateSysTenantDto dto);

    /// <summary>
    /// 更新租户
    /// </summary>
    Task<bool> UpdateAsync(UpdateSysTenantDto dto);

    /// <summary>
    /// 删除租户
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    Task<bool> TestConnectionAsync(string connectionString, int dbType);
}
