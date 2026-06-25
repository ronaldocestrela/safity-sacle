namespace SafetyScale.Domain.Entities;

public enum EmailQueueStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}
