namespace SafetyScale.Domain.Entities;

public enum BillingStatus
{
    None = 0,
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Incomplete = 5,
}
