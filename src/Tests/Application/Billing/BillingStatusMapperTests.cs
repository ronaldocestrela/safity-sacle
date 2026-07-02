using FluentAssertions;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Billing;

namespace SafetyScale.Tests.Application.Billing;

public class BillingStatusMapperTests
{
    [Theory]
    [InlineData("trialing", BillingStatus.Trialing)]
    [InlineData("active", BillingStatus.Active)]
    [InlineData("past_due", BillingStatus.PastDue)]
    [InlineData("canceled", BillingStatus.Canceled)]
    [InlineData("incomplete", BillingStatus.Incomplete)]
    public void FromStripeSubscriptionStatus_MapsKnownValues(string input, BillingStatus expected)
    {
        BillingStatusMapper.FromStripeSubscriptionStatus(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(BillingStatus.Active, true)]
    [InlineData(BillingStatus.Trialing, true)]
    [InlineData(BillingStatus.PastDue, false)]
    public void IsSubscriptionActive_ReturnsExpected(BillingStatus status, bool expected)
    {
        BillingStatusMapper.IsSubscriptionActive(status).Should().Be(expected);
    }

    [Fact]
    public void ResolveLeadStatus_WhenActive_SetsContracted()
    {
        BillingStatusMapper.ResolveLeadStatus(BillingStatus.Active, LeadStatus.New)
            .Should().Be(LeadStatus.Contracted);
    }

    [Fact]
    public void ResolveLeadStatus_WhenCanceledFromContracted_SetsLost()
    {
        BillingStatusMapper.ResolveLeadStatus(BillingStatus.Canceled, LeadStatus.Contracted)
            .Should().Be(LeadStatus.Lost);
    }
}
