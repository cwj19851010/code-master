using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// External login binding for OAuth providers such as GitHub.
/// </summary>
public class SysExternalLogin : EntityBase
{
    public long UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderUserId { get; set; } = string.Empty;

    public string? ProviderLogin { get; set; }

    public string? ProviderEmail { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime? LastLoginTime { get; set; }
}
