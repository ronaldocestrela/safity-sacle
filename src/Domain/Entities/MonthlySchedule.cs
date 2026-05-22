namespace SafetyScale.Domain.Entities;

public class MonthlySchedule : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }

    public ICollection<ScheduleItem> Items { get; set; } = new List<ScheduleItem>();
}
