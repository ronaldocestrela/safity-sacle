namespace SafetyScale.Domain.Entities;

public class UnavailableDay : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SecurityGuardId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }

    public Tenant? Tenant { get; set; }
    public SecurityGuard? SecurityGuard { get; set; }
}
