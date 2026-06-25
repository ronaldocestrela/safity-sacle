namespace SafetyScale.Infrastructure.Messaging.Email;

public sealed class EmailQueueWorkerOptions
{
    public const string SectionName = "EmailQueue";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 10;

    public int MaxAttempts { get; set; } = 5;

    public int InitialRetryDelaySeconds { get; set; } = 30;

    public int MaxRetryDelaySeconds { get; set; } = 3600;

    public int StaleProcessingMinutes { get; set; } = 10;
}
