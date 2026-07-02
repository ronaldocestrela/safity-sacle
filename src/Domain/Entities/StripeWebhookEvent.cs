namespace SafetyScale.Domain.Entities;

/// <summary>
/// Tracks processed Stripe webhook events for idempotent handling.
/// </summary>
public sealed class StripeWebhookEvent
{
    public Guid Id { get; set; }

    public string StripeEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
