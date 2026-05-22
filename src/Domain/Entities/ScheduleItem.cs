namespace SafetyScale.Domain.Entities;

public class ScheduleItem : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MonthlyScheduleId { get; set; }
    public Guid SecurityGuardId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsWeekend { get; set; }

    public Tenant? Tenant { get; set; }
    public MonthlySchedule? MonthlySchedule { get; set; }
    public SecurityGuard? SecurityGuard { get; set; }
}
