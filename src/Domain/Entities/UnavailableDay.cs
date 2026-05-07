namespace SafetyScale.Domain.Entities;

public class UnavailableDay
{
    public Guid Id { get; set; }
    public Guid SecurityGuardId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }

    public SecurityGuard? SecurityGuard { get; set; }
}
