namespace SafetyScale.Infrastructure.Billing;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Default Stripe API version pinned by SafetyScale (dahlia).</summary>
    public const string DefaultApiVersion = "2026-06-24.dahlia";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Stripe API version used for server-side requests and expected on webhook events.
    /// Must match the webhook endpoint version in Stripe Dashboard / CLI.
    /// </summary>
    public string ApiVersion { get; set; } = DefaultApiVersion;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !string.IsNullOrWhiteSpace(WebhookSecret) &&
        IsValidSecretKey(SecretKey);

    public static bool IsValidSecretKey(string? secretKey) =>
        !string.IsNullOrWhiteSpace(secretKey) &&
        (secretKey.StartsWith("sk_", StringComparison.Ordinal) ||
         secretKey.StartsWith("rk_", StringComparison.Ordinal));
}
