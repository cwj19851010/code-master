using System.Security.Cryptography;
using System.Text;
using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace CodeMaster.Application.Services.Auth;

public interface IMcpTokenService : IApplicationService
{
    Task<List<McpTokenDto>> GetCurrentUserTokensAsync();

    Task<CreateMcpTokenResultDto> CreateTokenAsync(CreateMcpTokenDto input);

    Task<bool> RevokeTokenAsync(long id);

    Task<McpTokenPrincipalDto?> ValidateTokenAsync(string token, string? ipAddress = null);
}

public class McpTokenService : IMcpTokenService
{
    private const string TokenPrefix = "cm_pat_";

    private readonly IRepository<SysMcpToken> _tokenRepository;
    private readonly IRepository<SysUser> _userRepository;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISqlSugarClient _db;

    public McpTokenService(
        IRepository<SysMcpToken> tokenRepository,
        IRepository<SysUser> userRepository,
        IAuthService authService,
        IHttpContextAccessor httpContextAccessor,
        ISqlSugarClient db)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<List<McpTokenDto>> GetCurrentUserTokensAsync()
    {
        var userId = GetCurrentUserId();
        var tokens = await _tokenRepository.GetQueryable()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();

        var now = DateTime.UtcNow;
        return tokens.Select(x => ToDto(x, now)).ToList();
    }

    public async Task<CreateMcpTokenResultDto> CreateTokenAsync(CreateMcpTokenDto input)
    {
        var userId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Current user was not found.");

        var name = string.IsNullOrWhiteSpace(input.Name) ? "CodeMaster MCP" : input.Name.Trim();
        if (name.Length > 100)
            name = name[..100];

        var rawToken = TokenPrefix + CreateRandomToken();
        var token = new SysMcpToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Name = name,
            TokenHash = HashToken(rawToken),
            TokenPrefix = rawToken[..Math.Min(rawToken.Length, 16)],
            Scopes = string.IsNullOrWhiteSpace(input.Scopes) ? "codegen:read codegen:write project:operate" : input.Scopes.Trim(),
            ExpiresAt = ToUtc(input.ExpiresAt),
            CreateUserId = user.Id,
            CreateBy = user.UserName,
            CreateTime = DateTime.UtcNow
        };

        var id = await _tokenRepository.InsertAsync(token);

        return new CreateMcpTokenResultDto
        {
            Id = id,
            Token = rawToken,
            TokenPrefix = token.TokenPrefix,
            ExpiresAt = token.ExpiresAt
        };
    }

    public async Task<bool> RevokeTokenAsync(long id)
    {
        var userId = GetCurrentUserId();
        var token = await _tokenRepository.GetQueryable()
            .Where(x => x.Id == id && x.UserId == userId)
            .FirstAsync();

        if (token == null)
            return false;

        token.RevokedAt = DateTime.UtcNow;
        token.UpdateUserId = userId;
        token.UpdateTime = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(token);
        return true;
    }

    public async Task<McpTokenPrincipalDto?> ValidateTokenAsync(string token, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
            return null;

        var hash = HashToken(token.Trim());
        var entity = await _db.Queryable<SysMcpToken>()
            .ClearFilter()
            .Where(x => x.TokenHash == hash && !x.IsDeleted)
            .FirstAsync();

        if (entity == null || entity.RevokedAt.HasValue)
            return null;

        var now = DateTime.UtcNow;
        if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value <= now)
            return null;

        var user = await _db.Queryable<SysUser>()
            .ClearFilter()
            .Where(x => x.Id == entity.UserId && !x.IsDeleted && x.Status == 0)
            .FirstAsync();

        if (user == null)
            return null;

        var userInfo = await _authService.GetUserInfoAsync(user.Id);
        if (userInfo == null)
            return null;

        entity.LastUsedAt = now;
        entity.LastUsedIp = ipAddress;
        entity.UpdateTime = now;
        await _db.Updateable(entity).ExecuteCommandAsync();

        return new McpTokenPrincipalDto
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            UserName = user.UserName,
            NickName = user.NickName,
            Roles = userInfo.Roles,
            Permissions = userInfo.Permissions,
            DataScope = userInfo.DataScope,
            IsAdmin = userInfo.IsAdmin,
            IsHostAdmin = userInfo.IsHostAdmin,
            IsTenantAdmin = userInfo.IsTenantAdmin
        };
    }

    private long GetCurrentUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirst(global::System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(value, out var userId) && userId > 0)
            return userId;

        throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private static McpTokenDto ToDto(SysMcpToken token, DateTime now)
    {
        return new McpTokenDto
        {
            Id = token.Id,
            Name = token.Name,
            TokenPrefix = token.TokenPrefix,
            Scopes = token.Scopes,
            CreateTime = token.CreateTime,
            ExpiresAt = token.ExpiresAt,
            RevokedAt = token.RevokedAt,
            LastUsedAt = token.LastUsedAt,
            LastUsedIp = token.LastUsedIp,
            IsActive = !token.RevokedAt.HasValue && (!token.ExpiresAt.HasValue || token.ExpiresAt.Value > now)
        };
    }

    private static string CreateRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
