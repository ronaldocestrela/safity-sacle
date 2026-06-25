using FluentAssertions;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class EmailConfirmationLinkBuilderTests
{
    [Fact]
    public void Build_ShouldCreateConfirmEmailUrlWithEncodedQueryParams()
    {
        var link = EmailConfirmationLinkBuilder.Build(
            "http://localhost:4864/",
            "user-123",
            "token with spaces/+");

        link.Should().Be(
            "http://localhost:4864/confirm-email?userId=user-123&token=token%20with%20spaces%2F%2B");
    }

    [Fact]
    public void Build_ShouldRejectMissingWebBaseUrl()
    {
        var action = () => EmailConfirmationLinkBuilder.Build("", "user-123", "token");

        action.Should().Throw<InvalidOperationException>();
    }
}
