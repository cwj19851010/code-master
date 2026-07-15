namespace CodeMaster.Application.Dtos.Auth;

public class McpTokenDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public string? Scopes { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public string? LastUsedIp { get; set; }

    public bool IsActive { get; set; }
}

public class CreateMcpTokenDto
{
    public string Name { get; set; } = string.Empty;

    public string? Scopes { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class CreateMcpTokenResultDto
{
    public long Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}

public class McpTokenPrincipalDto
{
    public long UserId { get; set; }

    public long TenantId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();

    public int DataScope { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }
}
