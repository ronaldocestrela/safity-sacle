using Stripe;

namespace SafetyScale.Infrastructure.Billing;

public static class StripeClientConfigurator
{
    public static void Apply(StripeOptions options)
    {
        StripeConfiguration.ApiKey = options.SecretKey;
    }

    /// <summary>Pinned API version shipped with the installed Stripe.net package.</summary>
    public static string PinnedApiVersion => StripeConfiguration.ApiVersion;
}
