using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Pages.Auth;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class LoginEmailConfirmationPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Submit_WithUnconfirmedEmail_ShowsConfirmationMessage()
    {
        var services = PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/auth/login", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.LoginEmailNotConfirmedResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            });

        var cut = RenderComponent<Login>();

        cut.Find("input[type='email']").Input("user@test.com");
        cut.Find("input[type='password']").Input("Aa!23456z");
        cut.Find("button.submit").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent
                .Should()
                .Be("Confirme seu e-mail pelo link enviado antes de entrar.");
            services.Navigation.LastUri.Should().BeNull();
        });
    }
}
