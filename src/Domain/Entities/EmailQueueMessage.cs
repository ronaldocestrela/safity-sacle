namespace SafetyScale.Domain.Entities;

public class EmailQueueMessage
{
    public Guid Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public EmailQueueStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
