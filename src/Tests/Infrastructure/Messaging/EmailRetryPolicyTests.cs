using FluentAssertions;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class EmailRetryPolicyTests
{
    [Theory]
    [InlineData(1, 30, 3600, 30)]
    [InlineData(2, 30, 3600, 60)]
    [InlineData(3, 30, 3600, 120)]
    [InlineData(8, 30, 3600, 3600)]
    public void CalculateNextAvailableUtc_ShouldApplyExponentialBackoffWithCap(
        int attempts,
        int initialDelaySeconds,
        int maxDelaySeconds,
        int expectedDelaySeconds)
    {
        var utcNow = new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);

        var nextAttemptAt = EmailRetryPolicy.CalculateNextAvailableUtc(
            attempts,
            initialDelaySeconds,
            maxDelaySeconds,
            utcNow);

        nextAttemptAt.Should().Be(utcNow.AddSeconds(expectedDelaySeconds));
    }

    [Fact]
    public void CalculateNextAvailableUtc_ShouldRejectInvalidAttempts()
    {
        var utcNow = DateTime.UtcNow;

        var action = () => EmailRetryPolicy.CalculateNextAvailableUtc(0, 30, 3600, utcNow);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
