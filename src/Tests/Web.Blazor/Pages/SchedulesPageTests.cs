using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class SchedulesPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Mount_LoadsRosterOnInit()
    {
        var getCalled = false;
        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    getCalled = true;
                    return SchedulesPageTestHelper.NotFoundResponse();
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Supervisor);

        var cut = RenderPage();

        cut.WaitForAssertion(() => getCalled.Should().BeTrue());
    }

    [Fact]
    public void Admin_ShowsGenerateButton()
    {
        SchedulesPageTestHelper.Register(
            Services,
            SchedulesPageTestHelper.DefaultHandler,
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button.btn-primary").Should().NotBeEmpty();
            cut.Markup.Should().Contain("Gerar agendamento");
        });
    }

    [Fact]
    public void Supervisor_HidesGenerateButton()
    {
        SchedulesPageTestHelper.Register(
            Services,
            SchedulesPageTestHelper.DefaultHandler,
            roles: UserRole.Supervisor);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button.btn-primary").Should().BeEmpty();
            cut.Markup.Should().NotContain("Gerar agendamento");
        });
    }

    [Fact]
    public void WithLoadedSchedule_ShowsAssignments()
    {
        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    return SchedulesPageTestHelper.JsonResponse(SchedulesPageTestHelper.SampleScheduleJson);
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Supervisor);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Pat Smith");
            cut.Markup.Should().Contain("Alex Inactive");
            cut.Markup.Should().Contain("Primary");
            cut.Markup.Should().Contain("Final de semana");
            cut.Markup.Should().Contain("Inativo");
        });
    }

    [Fact]
    public void WithMissingSchedule_ShowsNotFoundBanner()
    {
        SchedulesPageTestHelper.Register(
            Services,
            SchedulesPageTestHelper.DefaultHandler,
            roles: UserRole.Supervisor);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("Nenhuma escala encontrada");
        });
    }

    [Fact]
    public void Admin_Generate_SubmitsPostAndReloads()
    {
        var getCallCount = 0;
        string? postBody = null;

        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    getCallCount++;
                    return getCallCount == 1
                        ? SchedulesPageTestHelper.NotFoundResponse()
                        : SchedulesPageTestHelper.JsonResponse(SchedulesPageTestHelper.SampleScheduleJson);
                }

                if (SchedulesPageTestHelper.IsScheduleGeneratePost(request))
                {
                    postBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return SchedulesPageTestHelper.GenerateSuccessResponse(SchedulesPageTestHelper.SampleScheduleId);
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() => getCallCount.Should().BeGreaterThanOrEqualTo(1));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            postBody.Should().NotBeNullOrEmpty();
            using var doc = JsonDocument.Parse(postBody!);
            doc.RootElement.GetProperty("month").GetInt32().Should().BeInRange(1, 12);
            doc.RootElement.GetProperty("year").GetInt32().Should().BeInRange(2000, 2100);
            getCallCount.Should().BeGreaterThanOrEqualTo(2);
            cut.Find("[role='alert']").TextContent.Should().Contain("Escala mensal gerada com sucesso");
            cut.Markup.Should().Contain("Pat Smith");
        });
    }

    [Fact]
    public void Admin_GenerateConflict_LoadsExistingSchedule()
    {
        var getCallCount = 0;

        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    getCallCount++;
                    return getCallCount == 1
                        ? SchedulesPageTestHelper.NotFoundResponse()
                        : SchedulesPageTestHelper.JsonResponse(SchedulesPageTestHelper.SampleScheduleJson);
                }

                if (SchedulesPageTestHelper.IsScheduleGeneratePost(request))
                {
                    return SchedulesPageTestHelper.ConflictResponse();
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() => getCallCount.Should().BeGreaterThanOrEqualTo(1));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            getCallCount.Should().BeGreaterThanOrEqualTo(2);
            cut.Find("[role='alert']").TextContent.Should().Contain("Escala já gerada para este mês e ano");
            cut.Markup.Should().Contain("Pat Smith");
        });
    }

    [Fact]
    public void SecurityGuard_WithEmptyItems_ShowsNoShiftsMessage()
    {
        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    return SchedulesPageTestHelper.JsonResponse(SchedulesPageTestHelper.EmptyItemsScheduleJson);
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.SecurityGuard);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Você não tem turnos atribuídos neste período.");
        });
    }

    [Fact]
    public void Supervisor_WithEmptyItems_ShowsNoAssignmentsMessage()
    {
        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    return SchedulesPageTestHelper.JsonResponse(SchedulesPageTestHelper.EmptyItemsScheduleJson);
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Supervisor);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Não há agendamentos para este agendamento.");
        });
    }

    [Fact]
    public void Admin_GenerateCoverageFailure_ShowsApiMessage()
    {
        const string message =
            "Não foi possível gerar a escala para 02/05/2026 porque não há seguranças elegíveis suficientes para cobrir todas as vagas do dia.";

        SchedulesPageTestHelper.Register(
            Services,
            request =>
            {
                if (SchedulesPageTestHelper.IsScheduleByMonthYearGet(request))
                {
                    return SchedulesPageTestHelper.NotFoundResponse();
                }

                if (SchedulesPageTestHelper.IsScheduleGeneratePost(request))
                {
                    return SchedulesPageTestHelper.CoverageFailureResponse(message);
                }

                return SchedulesPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.FindAll("button.btn-primary").Should().NotBeEmpty());

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role='alert']");
            alert.TextContent.Should().Contain("Não foi possível gerar a escala");
            alert.TextContent.Should().Contain("02/05/2026");
            alert.TextContent.Should().Contain("seguranças elegíveis");
        });
    }

    private IRenderedComponent<CascadingAuthenticationState> RenderPage() =>
        RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Schedules>());
}
