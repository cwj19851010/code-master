using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using CodeMaster.Domain.Entities.System;

namespace CodeMaster.Application.Services.Auth;

/// <summary>
/// JWT服务接口
/// </summary>
public interface IJwtService
{
    string GenerateToken(
        SysUser user,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        int? dataScope = null,
        long? deptId = null,
        string? deptAncestors = null,
        int? postDataScope = null,
        bool? isAdmin = null,
        bool? isHostAdmin = null,
        bool? isTenantAdmin = null);
    ClaimsPrincipal? ValidateToken(string token);
}

/// <summary>
/// JWT服务实现
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(
        SysUser user,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        int? dataScope = null,
        long? deptId = null,
        string? deptAncestors = null,
        int? postDataScope = null,
        bool? isAdmin = null,
        bool? isHostAdmin = null,
        bool? isTenantAdmin = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSetting("SecretKey")!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("NickName", user.NickName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 添加租户ID到Claims（0表示宿主）
        claims.Add(new Claim("TenantId", (user.TenantId == 0 ? 0 : user.TenantId).ToString()));

        // 角色
        if (roles != null)
        {
            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // 权限
        if (permissions != null)
        {
            foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                claims.Add(new Claim("Permission", permission));
            }
        }

        // 数据权限范围（角色）
        if (dataScope.HasValue)
        {
            claims.Add(new Claim("DataScope", dataScope.Value.ToString()));
        }

        // 职位数据权限范围
        if (postDataScope.HasValue)
        {
            claims.Add(new Claim("PostDataScope", postDataScope.Value.ToString()));
        }

        // 部门ID
        if (deptId.HasValue)
        {
            claims.Add(new Claim("DeptId", deptId.Value.ToString()));
        }

        // 部门路径（用于数据权限过滤）
        if (!string.IsNullOrEmpty(deptAncestors))
        {
            claims.Add(new Claim("DeptAncestors", deptAncestors));
        }

        // 是否管理员
        if (isAdmin.HasValue)
        {
            claims.Add(new Claim("IsAdmin", isAdmin.Value ? "true" : "false"));
        }

        if (isHostAdmin.HasValue)
        {
            claims.Add(new Claim("IsHostAdmin", isHostAdmin.Value ? "true" : "false"));
        }

        if (isTenantAdmin.HasValue)
        {
            claims.Add(new Claim("IsTenantAdmin", isTenantAdmin.Value ? "true" : "false"));
        }

        var token = new JwtSecurityToken(
            issuer: GetSetting("Issuer"),
            audience: GetSetting("Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(GetSetting("ExpirationMinutes") ?? "120")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetSetting("SecretKey")!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetSetting("Issuer"),
                ValidateAudience = true,
                ValidAudience = GetSetting("Audience"),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string? GetSetting(string key)
    {
        return _configuration[$"Jwt:{key}"] ?? _configuration[$"JwtSettings:{key}"];
    }
}
