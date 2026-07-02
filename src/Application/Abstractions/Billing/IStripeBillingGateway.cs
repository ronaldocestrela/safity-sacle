namespace SafetyScale.Application.Abstractions.Billing;

public interface IStripeBillingGateway
{
    bool IsConfigured { get; }

    Task<string> CreateCheckoutSessionAsync(
        StripeCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<string> CreatePortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    StripeWebhookParseResult ParseWebhookEvent(string jsonPayload, string stripeSignatureHeader);
}

public sealed record StripeCheckoutRequest(
    string? StripeCustomerId,
    string CustomerEmail,
    string StripePriceId,
    string SuccessUrl,
    string CancelUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeWebhookParseResult(
    bool IsValid,
    string? EventId = null,
    string? EventType = null,
    StripeWebhookEventData? EventData = null,
    string? ErrorMessage = null);

public sealed record StripeWebhookEventData(
    string? CustomerId,
    string? SubscriptionId,
    string? SubscriptionStatus,
    DateTime? CurrentPeriodEnd,
    Guid? TenantId,
    Guid? PlanId,
    string? CheckoutSessionId);
