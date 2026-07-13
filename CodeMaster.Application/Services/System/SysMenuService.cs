using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Core.Services;
using Mapster;
using CodeMaster.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CodeMaster.Core.Authorization;
using CodeMaster.Application.Services.Auth;
using Yitter.IdGenerator;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 菜单服务接口
/// </summary>
public interface ISysMenuService : ITreeApplicationService<SysMenu, SysMenuDto, SysMenuDto, SysMenuQueryDto, CreateSysMenuDto, UpdateSysMenuDto>
{
    Task<List<RouteDto>> GetUserRoutesAsync();
    Task<List<SysMenuDto>> GetMenuListAsync();
}

/// <summary>
/// 菜单服务实现
/// </summary>
[DynamicApiPermission(requirePermission: true)]
public class SysMenuService : TreeApplicationService<SysMenu, SysMenuDto, SysMenuDto, SysMenuQueryDto, CreateSysMenuDto, UpdateSysMenuDto>, ISysMenuService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadOnlyRepository<SysUserRole> _userRoleRepository;
    private readonly IReadOnlyRepository<SysRoleMenu> _roleMenuRepository;

    public SysMenuService(
        IRepository<SysMenu> repository,
        IExcelService excelService,
        ISqlSugarClient db,
        IHttpContextAccessor httpContextAccessor,
        IReadOnlyRepository<SysUserRole> userRoleRepository,
        IReadOnlyRepository<SysRoleMenu> roleMenuRepository,
        CodeMaster.Core.Services.ICacheService? cacheService = null)
        : base(repository, excelService, db, cacheService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRoleRepository = userRoleRepository;
        _roleMenuRepository = roleMenuRepository;
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    public override async Task<long> CreateAsync(CreateSysMenuDto input)
    {
        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    public override async Task<int> UpdateAsync(long id, UpdateSysMenuDto input)
    {
        var menu = await Repository.GetByIdAsync(id);
        if (menu == null)
        {
            throw new Exception($"菜单 {id} 不存在");
        }

        // 检查父级是否变更
        if (input.NewParentId != menu.ParentId)
        {
            await MoveNodeAsync(id, input.NewParentId ?? 0);
        }

        // 更新菜单字段
        menu.MenuName = input.MenuName;
        menu.TitleKey = input.TitleKey;
        menu.OrderNum = input.OrderNum;
        menu.Path = input.Path;
        menu.Component = input.Component;
        menu.Query = input.Query;
        menu.IsFrame = input.IsFrame;
        menu.IsCache = input.IsCache;
        menu.MenuType = input.MenuType;
        menu.Visible = input.Visible;
        menu.Status = input.Status;
        menu.Perms = input.Perms;
        menu.Icon = input.Icon;
        menu.MenuScope = input.MenuScope;
        menu.Remark = input.Remark;

        return await Repository.UpdateAsync(menu);
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    public override async Task<int> DeleteAsync(long id)
    {
        return await base.DeleteAsync(id);
    }

    /// <summary>
    /// 获取菜单列表（扁平结构，由前端构建树形）
    /// </summary>
    public async Task<List<SysMenuDto>> GetMenuListAsync()
    {
        return await GetListAsync(new SysMenuQueryDto());
    }

    protected override Task<ISugarQueryable<SysMenu>> CreateFilteredQueryAsync(SysMenuQueryDto input)
    {
        var queryable = (ISugarQueryable<SysMenu>)Repository.GetQueryable();

        queryable = queryable
            .WhereIF(!string.IsNullOrEmpty(input.MenuName), m => m.MenuName.Contains(input.MenuName))
            .WhereIF(input.Status.HasValue, m => m.Status == input.Status.Value)
            .OrderBy(m => m.ParentId)
            .OrderBy(m => m.OrderNum);

        return Task.FromResult(queryable);
    }

    public async Task<List<RouteDto>> GetUserRoutesAsync()
    {
        // 从 JWT Token 获取用户 ID 和租户 ID
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("TenantId");
        var isAdminClaim = _httpContextAccessor.HttpContext?.User.FindFirst("IsAdmin")?.Value;

        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            Console.WriteLine("[GetUserRoutes] 无法获取用户ID");
            return new List<RouteDto>();
        }

        Console.WriteLine($"[GetUserRoutes] UserId: {userId}");

        // 获取当前租户ID（0表示宿主）
        long currentTenantId = 0;
        if (tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out var tid))
        {
            currentTenantId = tid;
        }

        Console.WriteLine($"[GetUserRoutes] TenantId: {currentTenantId}");

        // 检查是否是 admin 角色
        bool isAdmin = bool.TryParse(isAdminClaim, out var parsedIsAdmin) && parsedIsAdmin;

        List<SysMenu> menus;

        if (isAdmin)
        {
            // admin 角色：获取所有菜单（不需要检查角色权限）
            Console.WriteLine("[GetUserRoutes] Admin角色，获取所有路由");

            var query = _db.Queryable<SysMenu>().ClearFilter();

            query = query.Where(m => m.Status == 0
                && (m.MenuType == "M" || m.MenuType == "C"));

            // 根据租户类型过滤菜单
            if (currentTenantId == 0)
            {
                // 宿主：可以看到宿主专用(0)和共享菜单(2)
                query = AdminPermissionHelper.ApplyMenuScope(query, currentTenantId);
            }
            else
            {
                // 租户：可以看到租户专用(1)和共享菜单(2)
                query = AdminPermissionHelper.ApplyMenuScope(query, currentTenantId);
            }

            menus = await query
                .OrderBy(m => m.ParentId)
                .OrderBy(m => m.OrderNum)
                .ToListAsync();
#if DEBUG
            var ss = menus.Select(t => new { t.Id, t.ParentId, t.Component }).ToList();
#endif
        }
        else
        {
            // 非 admin 角色：按照角色权限获取菜单
            // 1. 获取用户的角色ID列表
            var userRoles = await ((ISugarQueryable<SysUserRole>)_userRoleRepository.GetQueryable())
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            Console.WriteLine($"[GetUserRoutes] UserRoles count: {userRoles.Count}, Roles: {string.Join(",", userRoles)}");

            if (!userRoles.Any())
            {
                Console.WriteLine("[GetUserRoutes] 用户没有角色");
                return new List<RouteDto>();
            }

            // 2. 获取角色对应的菜单ID列表
            var roleMenus = await ((ISugarQueryable<SysRoleMenu>)_roleMenuRepository.GetQueryable())
                .Where(rm => userRoles.Contains(rm.RoleId))
                .Select(rm => rm.MenuId)
                .ToListAsync();

            Console.WriteLine($"[GetUserRoutes] RoleMenus count: {roleMenus.Count}");

            if (!roleMenus.Any())
            {
                Console.WriteLine("[GetUserRoutes] 角色没有菜单���限");
                return new List<RouteDto>();
            }

            // 3. 获取菜单列表（根据 MenuScope 过滤）
            // Visible=true 表示在菜单中显示，Visible=false 表示隐藏但可访问（如新增、编辑、详情页）
            var query = _db.Queryable<SysMenu>().ClearFilter();

            query = query.Where(m => roleMenus.Contains(m.Id)
                && m.Status == 0
                && (m.MenuType == "M" || m.MenuType == "C"));

            // 根据租户类型过滤菜单
            if (currentTenantId == 0)
            {
                // 宿主：可以看到宿主专用(0)和共享菜单(2)
                query = AdminPermissionHelper.ApplyMenuScope(query, currentTenantId);
            }
            else
            {
                // 租户：可以看到租户专用(1)和共享菜单(2)
                query = AdminPermissionHelper.ApplyMenuScope(query, currentTenantId);
            }

            menus = await query
                .OrderBy(m => m.ParentId)
                .OrderBy(m => m.OrderNum)
                .ToListAsync();
        }

        // 4. 构建路由树（会自动提升子页面到父级，使用绝对路径）
        var routeTree = new List<RouteDto>();
        BuildRouteTree(menus, null, "", routeTree);

        return routeTree;
    }

    /// <summary>
    /// 构建路由树（保持完整的树形结构）
    /// </summary>
    private void BuildRouteTree(List<SysMenu> menus, long? parentId, string parentPath, List<RouteDto> routes)
    {
        var children = menus.Where(m => m.ParentId == parentId).ToList();

#if DEBUG
        Console.WriteLine($"[BuildRouteTree] ParentId={parentId}, ParentPath={parentPath}, Children count={children.Count}");
        foreach (var child in children)
        {
            Console.WriteLine($"  - Menu: Id={child.Id}, Name={child.MenuName}, Path={child.Path}, Type={child.MenuType}, ParentId={child.ParentId}");
        }
#endif
        foreach (var menu in children)
        {
            var currentPath = (menu.Path ?? "").TrimStart('/');
            var fullPath = string.IsNullOrEmpty(parentPath)
                ? "/" + currentPath
                : $"{parentPath}/{currentPath}";

            RouteDto route;

            // 如果当前菜单是目录(MenuType=M)，作为 Layout 容器
            if (menu.MenuType == "M")
            {
                // 为 Layout 路由生成唯一的名称，避免命名冲突
                // 例如：/system -> System, /monitor -> Monitor
                var layoutName = fullPath.TrimStart('/').Replace("/", "");
                if (string.IsNullOrEmpty(layoutName))
                {
                    layoutName = "Layout";
                }
                else
                {
                    // 首字母大写
                    layoutName = char.ToUpper(layoutName[0]) + layoutName.Substring(1);
                }

                route = new RouteDto
                {
                    Path = fullPath,
                    Name = layoutName,
                    Component = "Layout",
                    Meta = new RouteMetaDto
                    {
                        Title = string.IsNullOrWhiteSpace(menu.TitleKey) ? menu.MenuName : menu.TitleKey,
                        Icon = menu.Icon,
                        Hidden = !menu.Visible,
                        NoCache = !menu.IsCache,
                        Permission = menu.Perms
                    },
                    Children = new List<RouteDto>()
                };
            }
            else
            {
                // MenuType=C 的菜单
                route = new RouteDto
                {
                    Path = fullPath,
                    Name = GenerateComponentName(menu.Component),
                    Component = menu.Component,
                    Meta = new RouteMetaDto
                    {
                        Title = string.IsNullOrWhiteSpace(menu.TitleKey) ? menu.MenuName : menu.TitleKey,
                        Icon = menu.Icon,
                        Hidden = !menu.Visible,
                        NoCache = !menu.IsCache,
                        Permission = menu.Perms
                    },
                    Children = new List<RouteDto>()
                };
            }

            // 递归获取子路由，直接添加到当前路由的 Children 中
            BuildRouteTree(menus, menu.Id, fullPath, route.Children);

            // 如果没有子路由，设置 Children 为 null
            if (route.Children.Count == 0)
            {
                route.Children = null;
            }

            // 添加到父级路由列表
            routes.Add(route);
        }
    }

    /// <summary>
    /// 根据组件路径生成组件名称
    /// 例如: system/user/index -> SystemUser
    ///      system/user/add -> SystemUserAdd
    /// </summary>
    private string GenerateComponentName(string? componentPath)
    {
        if (string.IsNullOrWhiteSpace(componentPath))
            return string.Empty;

        // 移除 .vue 后缀
        componentPath = componentPath.Replace(".vue", "");

        // 分割路径
        var parts = componentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // 转换为 PascalCase
        var name = string.Join("", parts.Select(p =>
        {
            // 移除 index，因为 index 是默认页面
            if (p.Equals("index", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // 首字母大写
            return char.ToUpper(p[0]) + p.Substring(1);
        }));

        return name;
    }
}
