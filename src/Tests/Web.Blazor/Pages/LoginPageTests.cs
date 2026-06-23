using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Pages.Auth;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class LoginPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Submit_WithSuccessfulLogin_NavigatesToApp()
    {
        var services = PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/auth/login", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.LoginSuccessResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            });

        var cut = RenderComponent<Login>();

        cut.Find("input[type='email']").Input("user@test.com");
        cut.Find("input[type='password']").Input("Aa!23456z");
        cut.Find("button.submit").Click();

        cut.WaitForAssertion(() =>
        {
            services.Navigation.LastUri.Should().Be("/app");
            services.Navigation.LastReplace.Should().BeTrue();
        });
    }

    [Fact]
    public void Submit_WithUnauthorized_ShowsInvalidCredentialsMessage()
    {
        var services = PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/auth/login", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.LoginUnauthorizedResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            });

        var cut = RenderComponent<Login>();

        cut.Find("input[type='email']").Input("user@test.com");
        cut.Find("input[type='password']").Input("wrong");
        cut.Find("button.submit").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Be("E-mail ou senha inválidos.");
            services.Navigation.LastUri.Should().BeNull();
        });
    }
}
