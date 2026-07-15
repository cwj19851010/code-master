using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// Personal access token used by CodeMaster MCP clients.
/// </summary>
public class SysMcpToken : EntityBase
{
    public long UserId { get; set; }

    public long TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public string? Scopes { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public string? LastUsedIp { get; set; }
}
