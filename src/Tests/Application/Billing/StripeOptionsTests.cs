using FluentAssertions;
using SafetyScale.Infrastructure.Billing;

namespace SafetyScale.Tests.Application.Billing;

public class StripeOptionsTests
{
    [Theory]
    [InlineData("sk_test_abc", true)]
    [InlineData("rk_test_abc", true)]
    [InlineData("pk_test_abc", false)]
    [InlineData("", false)]
    public void IsValidSecretKey_ValidatesPrefix(string key, bool expected)
    {
        StripeOptions.IsValidSecretKey(key).Should().Be(expected);
    }

    [Fact]
    public void IsConfigured_RequiresValidSecretKeyAndWebhookSecret()
    {
        var options = new StripeOptions
        {
            SecretKey = "pk_test_invalid",
            WebhookSecret = "whsec_test",
        };

        options.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void PinnedApiVersion_MatchesDahliaRelease()
    {
        StripeClientConfigurator.PinnedApiVersion.Should().Be("2026-06-24.dahlia");
    }
}
