using Microsoft.AspNetCore.Identity;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Identity;

public class AppUser : IdentityUser, ITenantOwnedEntity
{
    /// <summary>Organization this user belongs to (single tenant per user).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Display name shown in the UI (tenant admin full name).</summary>
    public string DisplayName { get; set; } = string.Empty;

    public Tenant? Tenant { get; set; }
}
