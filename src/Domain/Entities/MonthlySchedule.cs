namespace SafetyScale.Domain.Entities;

public class MonthlySchedule
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ScheduleItem> Items { get; set; } = new List<ScheduleItem>();
}
