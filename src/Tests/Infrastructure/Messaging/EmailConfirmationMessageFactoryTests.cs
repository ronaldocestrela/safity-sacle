using FluentAssertions;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class EmailConfirmationMessageFactoryTests
{
    [Fact]
    public void Create_ShouldIncludeConfirmationLinkInHtmlAndText()
    {
        const string link = "http://localhost/confirm-email?userId=u1&token=t1";

        var message = EmailConfirmationMessageFactory.Create(
            "user@example.com",
            "Maria <Admin>",
            link);

        message.To.Should().Be("user@example.com");
        message.Subject.Should().Contain("Confirme seu e-mail");
        message.BodyHtml.Should().Contain("confirm-email?userId=u1&amp;token=t1");
        message.BodyText.Should().Contain(link);
        message.BodyHtml.Should().Contain("Maria &lt;Admin&gt;");
    }
}
