namespace SafetyScale.Api.Contracts.Billing;

public sealed record BillingPlanResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors,
    bool HasStripePrice);

public sealed record TenantBillingStatusResponse(
    Guid TenantId,
    string BillingStatus,
    string LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName,
    DateTime? CurrentPeriodEnd,
    bool HasActiveSubscription,
    bool CanManageSubscription);

public sealed record CreateCheckoutSessionRequest(Guid PlanId);

public sealed record CheckoutSessionResponse(string CheckoutUrl);

public sealed record PortalSessionResponse(string PortalUrl);

public sealed record LinkPlanStripeRequest(string StripePriceId, string? StripeProductId);
