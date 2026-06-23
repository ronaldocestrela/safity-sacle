using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class UnavailableDaysPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Supervisor_WithLoadedCalendar_HidesAdminControlsAndDisablesDayButtons()
    {
        UnavailableDaysPageTestHelper.Register(
            Services,
            UnavailableDaysPageTestHelper.DefaultHandler,
            roles: UserRole.Supervisor);

        var cut = RenderPage();
        var dayKey = UnavailableDaysPageTestHelper.KeyDay1();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button.save-btn").Should().BeEmpty();
            cut.FindAll("#reason-optional").Should().BeEmpty();
            cut.Find($"button[aria-label='{dayKey}']").HasAttribute("disabled").Should().BeTrue();
        });
    }

    [Fact]
    public void Admin_WithForbiddenGuardsList_ShowsAlertMessage()
    {
        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.ForbiddenResponse("Sem permissão na API.");
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("Sem permissão na API.");
        });
    }

    [Fact]
    public void Admin_WithExistingUnavailableDay_ShowsUnavailTag()
    {
        var dayKey = UnavailableDaysPageTestHelper.KeyDay5();
        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.DefaultGuardsResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDaysListGet(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    return UnavailableDaysPageTestHelper.JsonResponse(
                        $$"""
                        [{
                          "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                          "securityGuardId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                          "date": "{{dayKey}}",
                          "reason": null
                        }]
                        """);
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("UNAVAIL"));
    }

    [Fact]
    public void Admin_WithPendingAdd_SubmitsSaveAndRefreshesList()
    {
        var dayKey = UnavailableDaysPageTestHelper.KeyDay1();
        string? addBody = null;
        var listCallCount = 0;

        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.DefaultGuardsResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDaysListGet(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    listCallCount++;
                    return UnavailableDaysPageTestHelper.EmptyDaysResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDayAddPost(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    addBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return UnavailableDaysPageTestHelper.CreatedDayResponse(Guid.NewGuid());
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            cut.Find($"button[aria-label='{dayKey}']").HasAttribute("disabled").Should().BeFalse());

        cut.Find($"button[aria-label='{dayKey}']").Click();
        cut.Find("#reason-optional").Change("Conference");
        cut.Find("button.save-btn").Click();

        cut.WaitForAssertion(() =>
        {
            addBody.Should().NotBeNullOrEmpty();
            using var doc = JsonDocument.Parse(addBody!);
            doc.RootElement.GetProperty("date").GetString().Should().Be(dayKey);
            doc.RootElement.GetProperty("reason").GetString().Should().Be("Conference");
            listCallCount.Should().BeGreaterThanOrEqualTo(2);
        });
    }

    [Fact]
    public void Admin_WithPendingRemove_SubmitsDeleteOnly()
    {
        var dayKey = UnavailableDaysPageTestHelper.KeyDay5();
        const string removeId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
        var addCalled = false;
        string? deletedPath = null;

        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.DefaultGuardsResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDaysListGet(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    return UnavailableDaysPageTestHelper.JsonResponse(
                        $$"""
                        [{
                          "id": "{{removeId}}",
                          "securityGuardId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                          "date": "{{dayKey}}",
                          "reason": null
                        }]
                        """);
                }

                if (UnavailableDaysPageTestHelper.IsDayAddPost(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    addCalled = true;
                    return UnavailableDaysPageTestHelper.CreatedDayResponse(Guid.NewGuid());
                }

                if (UnavailableDaysPageTestHelper.IsDayDelete(request))
                {
                    deletedPath = request.RequestUri!.AbsolutePath;
                    return UnavailableDaysPageTestHelper.NoContentResponse();
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            cut.Find($"button[aria-label='{dayKey}']").HasAttribute("disabled").Should().BeFalse());

        cut.Find($"button[aria-label='{dayKey}']").Click();
        cut.Find("button.save-btn").Click();

        cut.WaitForAssertion(() =>
        {
            deletedPath.Should().Be($"/api/unavailable-days/{removeId}");
            addCalled.Should().BeFalse();
        });
    }

    [Fact]
    public void Admin_WithDuplicateDateError_ShowsAlertMessage()
    {
        var dayKey = UnavailableDaysPageTestHelper.KeyDay1();

        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.DefaultGuardsResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDaysListGet(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    return UnavailableDaysPageTestHelper.EmptyDaysResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDayAddPost(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    return UnavailableDaysPageTestHelper.ConflictResponse("Duplicate date");
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            cut.Find($"button[aria-label='{dayKey}']").HasAttribute("disabled").Should().BeFalse());

        cut.Find($"button[aria-label='{dayKey}']").Click();
        cut.Find("button.save-btn").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("Duplicate date");
        });
    }

    [Fact]
    public void Admin_WithDaysLoadError_ShowsAlertMessage()
    {
        UnavailableDaysPageTestHelper.Register(
            Services,
            request =>
            {
                if (UnavailableDaysPageTestHelper.IsGuardsListGet(request))
                {
                    return UnavailableDaysPageTestHelper.DefaultGuardsResponse();
                }

                if (UnavailableDaysPageTestHelper.IsDaysListGet(request, UnavailableDaysPageTestHelper.DefaultGuardId))
                {
                    return UnavailableDaysPageTestHelper.ForbiddenResponse("No access");
                }

                return UnavailableDaysPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("No access");
        });
    }

    private IRenderedComponent<CascadingAuthenticationState> RenderPage() =>
        RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<UnavailableDays>());
}
