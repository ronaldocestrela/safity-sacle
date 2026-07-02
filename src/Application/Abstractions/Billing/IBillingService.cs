using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Billing;

public sealed record BillingPlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors,
    bool HasStripePrice);

public sealed record TenantBillingStatusDto(
    Guid TenantId,
    BillingStatus BillingStatus,
    LeadStatus LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName,
    DateTime? CurrentPeriodEnd,
    bool HasActiveSubscription,
    bool CanManageSubscription);

public sealed record CreateCheckoutSessionInput(Guid PlanId);

public enum CreateCheckoutSessionStatus
{
    Success,
    TenantNotFound,
    PlanNotFound,
    PlanInactive,
    PlanNotConfigured,
    StripeNotConfigured,
    StripeError,
}

public sealed record CreateCheckoutSessionResult(
    CreateCheckoutSessionStatus Status,
    string? CheckoutUrl = null,
    string? ErrorMessage = null);

public enum CreatePortalSessionStatus
{
    Success,
    TenantNotFound,
    NoStripeCustomer,
    StripeNotConfigured,
    StripeError,
}

public sealed record CreatePortalSessionResult(
    CreatePortalSessionStatus Status,
    string? PortalUrl = null,
    string? ErrorMessage = null);

public enum ProcessWebhookStatus
{
    Success,
    InvalidSignature,
    AlreadyProcessed,
    Ignored,
    ProcessingFailed,
}

public sealed record ProcessWebhookResult(
    ProcessWebhookStatus Status,
    string? ErrorMessage = null);

public sealed record LinkPlanStripeInput(string StripePriceId, string? StripeProductId);

public enum LinkPlanStripeStatus
{
    Success,
    NotFound,
    ValidationFailed,
}

public sealed record LinkPlanStripeResult(
    LinkPlanStripeStatus Status,
    IReadOnlyList<string>? Errors = null);

public interface IBillingService
{
    Task<IReadOnlyList<BillingPlanDto>> ListAvailablePlansAsync(CancellationToken cancellationToken = default);

    Task<TenantBillingStatusDto?> GetTenantBillingStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        string adminEmail,
        CreateCheckoutSessionInput input,
        CancellationToken cancellationToken = default);

    Task<CreatePortalSessionResult> CreatePortalSessionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ProcessWebhookResult> ProcessWebhookAsync(
        string jsonPayload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default);

    Task<LinkPlanStripeResult> LinkPlanStripeAsync(
        Guid planId,
        LinkPlanStripeInput input,
        CancellationToken cancellationToken = default);
}
