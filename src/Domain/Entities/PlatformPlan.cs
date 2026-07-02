namespace SafetyScale.Domain.Entities;

public sealed class PlatformPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Stable unique code for integrations and references.</summary>
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal PriceMonthly { get; set; }

    public int MaxSecurityGuards { get; set; }

    public int MaxSectors { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Stripe Product ID (prod_...).</summary>
    public string? StripeProductId { get; set; }

    /// <summary>Stripe Price ID (price_...) used for Checkout subscription mode.</summary>
    public string? StripePriceId { get; set; }
}
