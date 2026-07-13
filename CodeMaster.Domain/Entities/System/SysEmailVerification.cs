using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// Email verification code used by registration, binding, and password recovery.
/// </summary>
public class SysEmailVerification : EntityBase
{
    public string Email { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? IpAddress { get; set; }

    public int SendCount { get; set; } = 1;
}
