using Microsoft.AspNetCore.Authorization;

namespace CodeMaster.Agent.Services;

public interface IAiAuthorizationService
{
    Task<bool> HasAnyAsync(params string[] permissions);
    Task DemandAnyAsync(params string[] permissions);
}

internal sealed class AiAuthorizationService : IAiAuthorizationService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAiCurrentUser _currentUser;

    public AiAuthorizationService(
        IAuthorizationService authorizationService,
        IAiCurrentUser currentUser)
    {
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    public async Task<bool> HasAnyAsync(params string[] permissions)
    {
        if (permissions.Length == 0)
        {
            return true;
        }

        var policyName = permissions.Length == 1
            ? $"Permission:{permissions[0]}"
            : $"PermissionOr:{string.Join(',', permissions)}";
        var result = await _authorizationService.AuthorizeAsync(_currentUser.Principal, null, policyName);
        return result.Succeeded;
    }

    public async Task DemandAnyAsync(params string[] permissions)
    {
        if (!await HasAnyAsync(permissions))
        {
            throw new UnauthorizedAccessException("You do not have permission to perform this Agent operation.");
        }
    }
}
