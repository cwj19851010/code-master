using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CodeMaster.Agent.Services;

public interface IAiCurrentUser
{
    ClaimsPrincipal Principal { get; }
    long UserId { get; }
    long TenantId { get; }
    string UserName { get; }
}

internal sealed class AiCurrentUser : IAiCurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AiCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal Principal => _httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    public long UserId => GetRequiredLongClaim(ClaimTypes.NameIdentifier);

    public long TenantId => GetRequiredLongClaim("TenantId", allowZero: true);

    public string UserName => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

    private long GetRequiredLongClaim(string claimType, bool allowZero = false)
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        if (long.TryParse(value, out var result) && (allowZero || result > 0))
        {
            return result;
        }

        throw new UnauthorizedAccessException("User is not authenticated.");
    }
}
