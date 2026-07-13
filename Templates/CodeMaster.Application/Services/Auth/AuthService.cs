using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using SqlSugar;
using CodeMaster.Core.Data;
using CodeMaster.Core.Entities;
using Microsoft.AspNetCore.Http;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Application.Services.Auth;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(LoginDto loginDto);
    Task<UserInfoDto?> GetCurrentUserAsync(long userId);
    Task<UserInfoDto?> GetUserInfoAsync(long userId);
}

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly IRepository<SysUser> _userRepository;
    private readonly IReadOnlyRepository<SysDept> _deptRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly ISqlSugarClient _db;
    private readonly IDataPermissionContext? _dataPermissionContext;
    private readonly ISysLoginLogService _loginLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IRepository<SysUser> userRepository,
        IReadOnlyRepository<SysDept> deptRepository,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        ISqlSugarClient db,
        ISysLoginLogService loginLogService,
        IHttpContextAccessor httpContextAccessor,
        IDataPermissionContext? dataPermissionContext = null)
    {
        _userRepository = userRepository;
        _deptRepository = deptRepository;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        _db = db;
        _dataPermissionContext = dataPermissionContext;
        _loginLogService = loginLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
    {
        // 记录接收到的参数
        _logger.LogInformation("LoginAsync called with UserName={UserName}, Password={Password}",
            loginDto?.UserName ?? "NULL",
            string.IsNullOrEmpty(loginDto?.Password) ? "EMPTY" : "***");

        var loginIp = GetClientIp();
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "";
        var browser = ParseBrowser(userAgent);
        var os = ParseOS(userAgent);

        try
        {
            // 查询用户（登录时临时禁用数据权限过滤器）
            SysUser? user;

            // 使用 SqlSugar 的 ClearAndBackup 方法临时禁用过滤器
            _db.QueryFilter.ClearAndBackup<IDept>();
            try
            {
                var users = await _userRepository.GetListAsync(u => u.UserName == loginDto.UserName || u.Email == loginDto.UserName);
                user = users.FirstOrDefault();
            }
            finally
            {
                // 恢复过滤器
                _db.QueryFilter.Restore();
            }

            if (user == null)
            {
                await RecordLoginLogAsync(loginDto.UserName, loginIp, browser, os, 1, "用户名或密码错误");
                throw new Exception("用户名或密码错误");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                await RecordLoginLogAsync(loginDto.UserName, loginIp, browser, os, 1, "用户名或密码错误");
                throw new Exception("用户名或密码错误");
            }

            // 检查用户状态
            if (user.Status != 0)
            {
                await RecordLoginLogAsync(loginDto.UserName, loginIp, browser, os, 1, "用户已被停用");
                throw new Exception("用户已被停用");
            }

            // 获取角色与权限
            var roleInfo = await GetRoleInfoAsync(user);
            var postDataScope = await GetPostDataScopeAsync(user.PostId);

            // 获取用户部门路径
            string? deptAncestors = null;
            if (user.DeptId.HasValue)
            {
                var dept = await _deptRepository.GetByIdAsync(user.DeptId.Value);
                deptAncestors = dept?.Ancestors;
            }

            // 生成Token
            var token = _jwtService.GenerateToken(
                user,
                roleInfo.Roles,
                roleInfo.Permissions,
                roleInfo.DataScope,
                user.DeptId,
                deptAncestors,
                postDataScope,
                roleInfo.IsAdmin,
                roleInfo.IsHostAdmin,
                roleInfo.IsTenantAdmin);
            var expireHours = Convert.ToInt32(GetJwtSetting("ExpireHours"));

            // 记录登录成功日志
            await RecordLoginLogAsync(loginDto.UserName, loginIp, browser, os, 0, "登录成功");

            return new LoginResultDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = expireHours * 3600,
                UserInfo = new UserInfoDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    NickName = user.NickName,
                    Avatar = user.Avatar,
                    Roles = roleInfo.Roles,
                    Permissions = roleInfo.Permissions,
                    DataScope = roleInfo.DataScope,
                    IsAdmin = roleInfo.IsAdmin,
                    IsHostAdmin = roleInfo.IsHostAdmin,
                    IsTenantAdmin = roleInfo.IsTenantAdmin
                }
            };
        }
        catch (Exception ex)
        {
            // 如果还没记录失败日志，在这里记录
            if (!ex.Message.Contains("用户名或密码错误") && !ex.Message.Contains("用户已被停用"))
            {
                await RecordLoginLogAsync(loginDto.UserName, loginIp, browser, os, 1, ex.Message);
            }
            throw;
        }
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserInfoDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Avatar = user.Avatar ?? string.Empty,
            DeptId = user.DeptId
        };
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // 获取部门信息
        string? deptName = null;
        if (user.DeptId.HasValue)
        {
            var dept = await _deptRepository.GetByIdAsync(user.DeptId.Value);
            deptName = dept?.Name;
        }

        var roleInfo = await GetRoleInfoAsync(user);

        return new UserInfoDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            DeptId = user.DeptId,
            DeptName = deptName,
            Roles = roleInfo.Roles,
            Permissions = roleInfo.Permissions,
            DataScope = roleInfo.DataScope,
            IsAdmin = roleInfo.IsAdmin,
            IsHostAdmin = roleInfo.IsHostAdmin,
            IsTenantAdmin = roleInfo.IsTenantAdmin
        };
    }

    private async Task<(List<string> Roles, List<string> Permissions, int DataScope, bool IsAdmin, bool IsHostAdmin, bool IsTenantAdmin)> GetRoleInfoAsync(SysUser user)
    {
        // 获取用户角色
        var userRoles = await _db.Queryable<SysUserRole>()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var roles = new List<string>();
        var permissions = new List<string>();
        int dataScope = 1; // 默认全部数据权限

        if (userRoles.Any())
        {
            // 获取角色信息
            var roleList = await _db.Queryable<SysRole>()
                .ClearFilter()
                .Where(r => userRoles.Contains(r.Id))
                .ToListAsync();

            roles = roleList.Select(r => r.RoleKey).ToList();

            // 获取最小的数据权限范围（数字越大权限越小）
            dataScope = roleList.Max(r => r.DataScope);

            var isHostAdmin = AdminPermissionHelper.IsHostAdmin(user, roleList);
            var isTenantAdmin = AdminPermissionHelper.IsTenantAdmin(user, roleList);
            if (isHostAdmin || isTenantAdmin)
            {
                permissions = await AdminPermissionHelper.GetScopedPermissionsAsync(_db, user.TenantId);
                return (roles, permissions, dataScope, true, isHostAdmin, isTenantAdmin);
            }

            // 获取角色对应的菜单权限
            var roleIds = roleList.Select(r => r.Id).ToList();
            var roleMenus = await _db.Queryable<SysRoleMenu>()
                .ClearFilter()
                .Where(rm => roleIds.Contains(rm.RoleId))
                .Select(rm => rm.MenuId)
                .ToListAsync();

            if (roleMenus.Any())
            {
                // 获取菜单权限标识
                var menus = await _db.Queryable<SysMenu>()
                    .ClearFilter()
                    .Where(m => roleMenus.Contains(m.Id) && !string.IsNullOrEmpty(m.Perms))
                    .ToListAsync();

                permissions = menus
                    .Where(m => !string.IsNullOrEmpty(m.Perms))
                    .Where(m => user.TenantId == 0
                        ? m.MenuScope == AdminPermissionHelper.HostMenuScope || m.MenuScope == AdminPermissionHelper.SharedMenuScope
                        : m.MenuScope == AdminPermissionHelper.TenantMenuScope || m.MenuScope == AdminPermissionHelper.SharedMenuScope)
                    .Select(m => m.Perms!)
                    .Distinct()
                    .ToList();
            }
        }

        var fallbackHostAdmin = AdminPermissionHelper.IsHostAdmin(user, Enumerable.Empty<SysRole>());
        if (fallbackHostAdmin)
        {
            permissions = await AdminPermissionHelper.GetScopedPermissionsAsync(_db, user.TenantId);
            return (roles, permissions, dataScope, true, true, false);
        }

        return (roles, permissions, dataScope, false, false, false);
    }

    private async Task<int> GetPostDataScopeAsync(long? postId)
    {
        if (!postId.HasValue)
        {
            return 1; // 默认本人
        }

        var scope = await _db.Queryable<SysPost>()
            .Where(p => p.Id == postId.Value)
            .Select(p => p.DataScope)
            .FirstAsync();

        return scope == 0 ? 1 : scope;
    }

    private string? GetJwtSetting(string key)
    {
        return _configuration[$"Jwt:{key}"] ?? _configuration[$"JwtSettings:{key}"];
    }

    private string GetClientIp()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "Unknown";

        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Connection.RemoteIpAddress?.ToString();
        }
        return ip ?? "Unknown";
    }

    private string ParseBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        if (userAgent.Contains("Edge")) return "Edge";
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Opera")) return "Opera";
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) return "IE";

        return "Unknown";
    }

    private string ParseOS(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        if (userAgent.Contains("Windows NT 10.0")) return "Windows 10";
        if (userAgent.Contains("Windows NT 6.3")) return "Windows 8.1";
        if (userAgent.Contains("Windows NT 6.2")) return "Windows 8";
        if (userAgent.Contains("Windows NT 6.1")) return "Windows 7";
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac OS X")) return "Mac OS X";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";

        return "Unknown";
    }

    private async Task RecordLoginLogAsync(string userName, string loginIp, string browser, string os, int status, string msg)
    {
        try
        {
            var loginLog = new SysLoginLog
            {
                UserName = userName,
                LoginIp = loginIp,
                Browser = browser,
                Os = os,
                Status = status,
                Msg = msg,
                LoginTime = DateTime.UtcNow
            };

            await _loginLogService.InsertLoginLogAsync(loginLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login log for user {UserName}", userName);
        }
    }
}
