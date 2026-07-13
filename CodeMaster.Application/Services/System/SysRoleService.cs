using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Services;
using Mapster;
using SqlSugar;
using CodeMaster.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 角色服务接口
/// </summary>
public interface ISysRoleService : IApplicationService
{
    [Permission("system:role:view")]
    Task<SysRoleDto?> GetByIdAsync(long id);

    [Permission("system:role:list")]
    Task<PagedResultDto<SysRoleDto>> GetPagedListAsync(SysRoleQueryDto query);

    [Permission("system:role:create")]
    Task<long> CreateAsync(CreateSysRoleDto dto);

    [Permission("system:role:update")]
    Task<int> UpdateAsync(long id, UpdateSysRoleDto dto);

    [Permission("system:role:delete")]
    Task<int> DeleteAsync(long id);

    [Permission("system:role:list")]
    Task<List<SysRoleDto>> GetAllList();

    [Permission("system:role:view")]
    Task<List<long>> GetRoleMenuIdsAsync(long roleId);

    [Permission("system:role:update")]
    Task AssignMenusAsync(long roleId, List<long> menuIds);
}

/// <summary>
/// 角色服务实现
/// </summary>
public class SysRoleService : ISysRoleService
{
    private readonly IRepository<SysRole> _roleRepository;
    private readonly ISqlSugarClient _db;

    public SysRoleService(IRepository<SysRole> roleRepository, ISqlSugarClient db)
    {
        _roleRepository = roleRepository;
        _db = db;
    }

    public async Task<SysRoleDto?> GetByIdAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null) return null;

        // 使用 Mapster 映射
        return role.Adapt<SysRoleDto>();
    }

    public async Task<PagedResultDto<SysRoleDto>> GetPagedListAsync(SysRoleQueryDto query)
    {
        var queryable = _db.Queryable<SysRole>()
            .WhereIF(!string.IsNullOrWhiteSpace(query.RoleName), r => r.RoleName.Contains(query.RoleName!))
            .WhereIF(!string.IsNullOrWhiteSpace(query.RoleKey), r => r.RoleKey.Contains(query.RoleKey!))
            .WhereIF(query.Status.HasValue, r => r.Status == query.Status!.Value)
            .WhereIF(query.BeginTime.HasValue, r => r.CreateTime >= query.BeginTime!.Value)
            .WhereIF(query.EndTime.HasValue, r => r.CreateTime <= query.EndTime!.Value);

        RefAsync<int> total = 0;
        var items = await queryable.ToPageListAsync(query.PageNum, query.PageSize, total);

        // 使用 Mapster 批量映射
        var dtoList = items.Adapt<List<SysRoleDto>>();

        return new PagedResultDto<SysRoleDto>
        {
            Items = dtoList,
            Total = total.Value,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CreateAsync(CreateSysRoleDto dto)
    {
        var existRole = await _roleRepository.GetListAsync(r => r.RoleKey == dto.RoleKey);
        if (existRole.Any())
        {
            throw new Exception($"角色权限字符 {dto.RoleKey} 已存在");
        }

        // 使用 Mapster 映射
        var role = dto.Adapt<SysRole>();

        var roleId = await _roleRepository.InsertAsync(role);

        // 分配菜单
        if (dto.MenuIds != null && dto.MenuIds.Any())
        {
            await AssignMenusAsync(roleId, dto.MenuIds);
        }

        return roleId;
    }

    public async Task<int> UpdateAsync(long id, UpdateSysRoleDto dto)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new Exception($"角色ID {id} 不存在");
        }

        // 检查是否为 admin 角色（RoleKey 包含 admin）
        bool isAdminRole = IsProtectedAdminRole(role);

        if (isAdminRole)
        {
            throw new Exception($"管理员角色 '{role.RoleName}' (RoleKey: {role.RoleKey}) 不允许修改");
        }

        // 使用 Mapster 更新现有对象
        dto.Adapt(role);

        var result = await _roleRepository.UpdateAsync(role);

        // 更新菜单分配
        if (dto.MenuIds != null)
        {
            await AssignMenusAsync(id, dto.MenuIds);
        }

        return result;
    }

    public async Task<int> DeleteAsync(long id)
    {
        // 1. 获取角色信息
        var role = await _db.Queryable<SysRole>()
            .Where(r => r.Id == id)
            .FirstAsync();

        if (role == null)
        {
            throw new Exception("角色不存在");
        }

        // 2. 检查是否为 admin 角色
        bool isAdminRole = IsProtectedAdminRole(role);

        if (isAdminRole)
        {
            throw new Exception($"管理员角色 '{role.RoleName}' (RoleKey: {role.RoleKey}) 不允许删除");
        }

        return await _db.SoftDeleteAsync<SysRole>(id);
    }

    public async Task<List<SysRoleDto>> GetAllList()
    {
        var roles = await _db.Queryable<SysRole>()
            .Where(r => r.Status == 0)
            .OrderBy(r => r.RoleSort)
            .ToListAsync();

        return roles.Adapt<List<SysRoleDto>>();
    }

    public async Task<List<long>> GetRoleMenuIdsAsync(long roleId)
    {
        var menuIds = await _db.Queryable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .ToListAsync();

        return menuIds;
    }

    public async Task AssignMenusAsync(long roleId, List<long> menuIds)
    {
        // 检查角色是否存在
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            throw new Exception($"角色ID {roleId} 不存在");
        }

        // 检查是否为 admin 角色
        bool isAdminRole = IsProtectedAdminRole(role);
        if (isAdminRole)
        {
            throw new Exception($"管理员角色 '{role.RoleName}' (RoleKey: {role.RoleKey}) 不允许修改菜单权限");
        }

        // 删除旧的菜单关联
        await _db.Deleteable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId)
            .ExecuteCommandAsync();

        // 添加新的菜单关联
        if (menuIds != null && menuIds.Any())
        {
            var roleMenus = menuIds.Select(menuId => new SysRoleMenu
            {
                RoleId = roleId,
                MenuId = menuId
            }).ToList();

            await _db.Insertable(roleMenus).ExecuteCommandAsync();
        }
    }

    private static bool IsProtectedAdminRole(SysRole role)
    {
        return role.RoleKey.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || role.IsTenantAdmin;
    }
}
