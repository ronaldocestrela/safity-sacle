namespace SafetyScale.Domain.Entities;

public class SecurityGuardSector : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid SecurityGuardId { get; set; }
    public Guid SectorId { get; set; }

    public Tenant? Tenant { get; set; }
    public SecurityGuard? SecurityGuard { get; set; }
    public Sector? Sector { get; set; }
}
