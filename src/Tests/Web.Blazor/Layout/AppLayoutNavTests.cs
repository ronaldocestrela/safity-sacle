using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Layout;
using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Tests.Web.Blazor.Layout;

public sealed class AppLayoutNavTests : BlazorComponentTestBase
{
    [Fact]
    public void BottomNav_OnDashboardRoute_MarksDashboardActiveOnly()
    {
        RegisterAuthSessionServices("/app", UserRole.Admin);

        var cut = RenderComponent<AppLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>Body</p>"))));

        var links = cut.FindAll("nav.bottom-nav a");
        links.Should().HaveCount(5);

        var dashboard = links.Single(l => l.GetAttribute("href") == "/app");
        var sectors = links.Single(l => l.GetAttribute("href") == "/app/sectors");

        HasActiveClass(dashboard).Should().BeTrue();
        HasActiveClass(sectors).Should().BeFalse();
        links.Where(HasActiveClass).Should().ContainSingle();
    }

    [Fact]
    public void BottomNav_OnSectorsRoute_MarksSectorsActiveOnly()
    {
        RegisterAuthSessionServices("/app/sectors", UserRole.Supervisor);

        var cut = RenderComponent<AppLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>Body</p>"))));

        var links = cut.FindAll("nav.bottom-nav a");
        var dashboard = links.Single(l => l.GetAttribute("href") == "/app");
        var sectors = links.Single(l => l.GetAttribute("href") == "/app/sectors");

        HasActiveClass(dashboard).Should().BeFalse();
        HasActiveClass(sectors).Should().BeTrue();
        links.Where(HasActiveClass).Should().ContainSingle();
    }
}
