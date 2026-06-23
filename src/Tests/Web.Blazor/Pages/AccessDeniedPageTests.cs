using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class AccessDeniedPageTests : BlazorComponentTestBase
{
    [Fact]
    public void BackLink_PointsToAppDashboard()
    {
        RegisterAuthSessionServices("/app/access-denied", UserRole.Supervisor);

        var cut = RenderComponent<AccessDenied>();

        var link = cut.Find("a[href='/app']");
        link.TextContent.Should().Be("Voltar ao início");
    }
}
