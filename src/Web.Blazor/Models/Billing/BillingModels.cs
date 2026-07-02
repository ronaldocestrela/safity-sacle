namespace SafetyScale.Web.Blazor.Models.Billing;

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
    string BillingStatus,
    string LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName,
    DateTime? CurrentPeriodEnd,
    bool HasActiveSubscription,
    bool CanManageSubscription);

public sealed record CreateCheckoutSessionRequestDto(Guid PlanId);

public sealed record CheckoutSessionResponseDto(string CheckoutUrl);

public sealed record PortalSessionResponseDto(string PortalUrl);

public sealed record LinkPlanStripeRequestDto(string StripePriceId, string? StripeProductId);
