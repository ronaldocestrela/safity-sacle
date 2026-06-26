using Microsoft.AspNetCore.Identity;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    /// <summary>Distinguishes tenant operators from platform operators.</summary>
    public UserKind UserKind { get; set; } = UserKind.Tenant;

    /// <summary>Organization this user belongs to (single tenant per user). Null for platform users.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Linked security guard profile for tenant operators with the SecurityGuard role.</summary>
    public Guid? SecurityGuardId { get; set; }

    public Tenant? Tenant { get; set; }

    public SecurityGuard? SecurityGuard { get; set; }
}
