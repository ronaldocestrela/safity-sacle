using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Billing;

public static class BillingStatusMapper
{
    public static BillingStatus FromStripeSubscriptionStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "trialing" => BillingStatus.Trialing,
            "active" => BillingStatus.Active,
            "past_due" => BillingStatus.PastDue,
            "canceled" or "unpaid" => BillingStatus.Canceled,
            "incomplete" or "incomplete_expired" or "paused" => BillingStatus.Incomplete,
            _ => BillingStatus.None,
        };

    public static bool IsSubscriptionActive(BillingStatus status) =>
        status is BillingStatus.Active or BillingStatus.Trialing;

    public static LeadStatus ResolveLeadStatus(BillingStatus billingStatus, LeadStatus current) =>
        billingStatus switch
        {
            BillingStatus.Active or BillingStatus.Trialing => LeadStatus.Contracted,
            BillingStatus.Canceled or BillingStatus.Incomplete when current == LeadStatus.Contracted =>
                LeadStatus.Lost,
            _ => current,
        };
}
