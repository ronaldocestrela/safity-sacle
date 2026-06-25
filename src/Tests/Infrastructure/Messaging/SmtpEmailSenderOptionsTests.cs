using FluentAssertions;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class SmtpEmailSenderOptionsTests
{
    [Fact]
    public void ValidateOptions_ShouldFail_WhenHostMissing()
    {
        var options = new SmtpOptions
        {
            Host = "",
            FromAddress = "noreply@example.com"
        };

        var action = () => SmtpEmailSender.ValidateOptions(options);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*host*");
    }

    [Fact]
    public void ValidateOptions_ShouldFail_WhenFromAddressMissing()
    {
        var options = new SmtpOptions
        {
            Host = "smtp.example.com",
            FromAddress = ""
        };

        var action = () => SmtpEmailSender.ValidateOptions(options);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*from address*");
    }

    [Fact]
    public void ValidateOptions_ShouldFail_WhenPortInvalid()
    {
        var options = new SmtpOptions
        {
            Host = "smtp.example.com",
            FromAddress = "noreply@example.com",
            Port = 0
        };

        var action = () => SmtpEmailSender.ValidateOptions(options);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*port*");
    }
}
