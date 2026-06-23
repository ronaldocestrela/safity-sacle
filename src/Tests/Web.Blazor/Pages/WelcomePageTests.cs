using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class WelcomePageTests : BlazorComponentTestBase
{
    [Fact]
    public void AuthenticatedUser_ShowsSessionEmail()
    {
        AppDashboardTestHelper.Register(Services, "/app", UserRole.Admin);

        var cut = RenderComponent<Welcome>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("section[aria-label='Dados da sessão']").TextContent
                .Should()
                .Contain("user@example.com");
        });
    }
}
