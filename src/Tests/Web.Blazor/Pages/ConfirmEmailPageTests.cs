using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Pages.Auth;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class ConfirmEmailPageTests : BlazorComponentTestBase
{
    [Fact]
    public void ConfirmEmail_WithSuccess_ShowsSuccessMessage()
    {
        PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/auth/confirm-email", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.ConfirmEmailSuccessResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            },
            initialUri: "/confirm-email?userId=u1&token=t1");

        var cut = RenderComponent<ConfirmEmail>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".banner").TextContent.Should().Contain("confirmado");
            cut.Find("a.link-button").GetAttribute("href").Should().Be("/login");
        });
    }

    [Fact]
    public void ConfirmEmail_WithInvalidToken_ShowsError()
    {
        PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/auth/confirm-email", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.ConfirmEmailInvalidResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            },
            initialUri: "/confirm-email?userId=u1&token=bad");

        var cut = RenderComponent<ConfirmEmail>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("inválido");
        });
    }

    [Fact]
    public void ConfirmEmail_WithoutQueryParams_ShowsInvalidLinkMessage()
    {
        PublicAuthTestHelper.Register(
            Services,
            _ => PublicAuthTestHelper.NotFoundResponse(),
            initialUri: "/confirm-email");

        var cut = RenderComponent<ConfirmEmail>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("inválido");
        });
    }
}
