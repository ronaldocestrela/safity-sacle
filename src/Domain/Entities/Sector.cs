namespace SafetyScale.Domain.Entities;

public class Sector : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Positions to fill daily for schedule generation.</summary>
    public int RequiredGuardsPerDay { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
    public ICollection<SecurityGuardSector> SecurityGuardSectors { get; set; } = new List<SecurityGuardSector>();
}
