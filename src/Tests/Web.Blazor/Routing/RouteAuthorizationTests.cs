using Bunit.TestDoubles;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor;

namespace SafetyScale.Tests.Web.Blazor.Routing;

public sealed class RouteAuthorizationTests : BlazorComponentTestBase
{
    public RouteAuthorizationTests()
    {
        var auth = new TestAuthorizationContext();
        auth.RegisterAuthorizationServices(Services);
        auth.SetNotAuthorized();
    }

    [Fact]
    public void UnauthenticatedUser_OnApp_RedirectsToLoginWithReturnUrl()
    {
        var nav = RegisterNavigation("/app");

        var cut = RenderComponent<App>();

        nav.LastUri.Should().Be("/login?returnUrl=%2Fapp");
        nav.LastReplace.Should().BeTrue();
        cut.Markup.Should().NotContain("Dashboard");
    }
}
