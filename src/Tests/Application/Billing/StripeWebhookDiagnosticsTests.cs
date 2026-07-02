using FluentAssertions;
using SafetyScale.Infrastructure.Billing;

namespace SafetyScale.Tests.Application.Billing;

public class StripeWebhookDiagnosticsTests
{
    [Fact]
    public void TryExtractApiVersion_ReturnsVersionFromPayload()
    {
        const string json = """
            {
              "id": "evt_123",
              "type": "checkout.session.completed",
              "api_version": "2026-06-24.dahlia"
            }
            """;

        StripeWebhookDiagnostics.TryExtractApiVersion(json)
            .Should().Be("2026-06-24.dahlia");
    }

    [Fact]
    public void TryExtractEventMetadata_ReturnsIdAndType()
    {
        const string json = """
            {
              "id": "evt_abc",
              "type": "customer.subscription.updated",
              "api_version": "2026-06-24.dahlia"
            }
            """;

        StripeWebhookDiagnostics.TryExtractEventId(json).Should().Be("evt_abc");
        StripeWebhookDiagnostics.TryExtractEventType(json).Should().Be("customer.subscription.updated");
    }

    [Fact]
    public void TryExtractApiVersion_WithInvalidJson_ReturnsNull()
    {
        StripeWebhookDiagnostics.TryExtractApiVersion("{not-json").Should().BeNull();
    }
}
