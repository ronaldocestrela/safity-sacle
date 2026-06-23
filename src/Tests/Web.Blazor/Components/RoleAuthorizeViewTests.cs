using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Components;
using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Tests.Web.Blazor.Components;

public sealed class RoleAuthorizeViewTests : BlazorComponentTestBase
{
    [Fact]
    public void Supervisor_OnAdminOnlyGate_RedirectsToAccessDeniedAndHidesContent()
    {
        RegisterAuthenticatedAuth(UserRole.Supervisor);
        var nav = RegisterNavigation("/app/security-guards");

        var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<RoleAuthorizeView>(roleParams => roleParams
                .Add(p => p.AllowedRoles, (IReadOnlyList<UserRole>)[UserRole.Admin])
                .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p id=\"protected\">Protected</p>"))));

        nav.LastUri.Should().Be("/app/access-denied");
        nav.LastReplace.Should().BeTrue();
        cut.Markup.Should().NotContain("Protected");
    }

    [Fact]
    public void Admin_OnAdminOnlyGate_RendersProtectedContent()
    {
        RegisterAuthenticatedAuth(UserRole.Admin);
        RegisterNavigation("/app/security-guards");

        var cut = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<RoleAuthorizeView>(roleParams => roleParams
                .Add(p => p.AllowedRoles, (IReadOnlyList<UserRole>)[UserRole.Admin])
                .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p id=\"protected\">Protected</p>"))));

        cut.Find("#protected").TextContent.Should().Be("Protected");
    }
}
