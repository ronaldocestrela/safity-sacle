namespace SafetyScale.Domain.Entities;

public class SecurityGuard
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UnavailableDay> UnavailableDays { get; set; } = new List<UnavailableDay>();
    public ICollection<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();
}
