using System.Security.Claims;
using CodeMaster.Application.Services.Auth;

namespace CodeMaster.WebApi.Middleware;

public class McpTokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public McpTokenAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMcpTokenService mcpTokenService)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer cm_pat_", StringComparison.Ordinal))
        {
            var token = authorization["Bearer ".Length..].Trim();
            var principal = await mcpTokenService.ValidateTokenAsync(token, GetClientIp(context));
            if (principal != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, principal.UserId.ToString()),
                    new(ClaimTypes.Name, principal.UserName),
                    new("TenantId", principal.TenantId.ToString()),
                    new("DeptId", string.Empty),
                    new("PostDataScope", principal.DataScope.ToString()),
                    new("IsAdmin", principal.IsAdmin.ToString()),
                    new("IsHostAdmin", principal.IsHostAdmin.ToString()),
                    new("IsTenantAdmin", principal.IsTenantAdmin.ToString())
                };

                foreach (var role in principal.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                    claims.Add(new Claim("Role", role));
                }

                foreach (var permission in principal.Permissions)
                {
                    claims.Add(new Claim("Permission", permission));
                }

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "CodeMasterMcpToken"));
            }
        }

        await _next(context);
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
