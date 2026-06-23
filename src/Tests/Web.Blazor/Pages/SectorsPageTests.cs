using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class SectorsPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Supervisor_WithLoadedList_HidesWriteControls()
    {
        SectorsPageTestHelper.Register(
            Services,
            request => SectorsPageTestHelper.IsSectorsListGet(request)
                ? SectorsPageTestHelper.DefaultListResponse()
                : SectorsPageTestHelper.NotFoundResponse(),
            roles: UserRole.Supervisor);

        var cut = RenderSectorsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("section[aria-label='Lista de setores']").TextContent.Should().Contain("Perimeter");
            cut.Markup.Should().Contain("2 positions/day");
            cut.FindAll("button.fab").Should().BeEmpty();
            cut.FindAll("button.name-btn").Should().BeEmpty();
            cut.Find("button[role='switch'][aria-label='Status ativo para Perimeter']")
                .HasAttribute("disabled")
                .Should()
                .BeTrue();
        });
    }

    [Fact]
    public void Admin_SubmitCreate_RefreshesListWithNewSector()
    {
        var listCallCount = 0;
        string? postBody = null;
        var newId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        SectorsPageTestHelper.Register(
            Services,
            request =>
            {
                if (SectorsPageTestHelper.IsSectorsListGet(request))
                {
                    listCallCount++;
                    return listCallCount == 1
                        ? SectorsPageTestHelper.DefaultListResponse()
                        : SectorsPageTestHelper.SectorListResponse(SectorsPageTestHelper.LobbyAndPerimeterJson);
                }

                if (SectorsPageTestHelper.IsSectorsCreatePost(request))
                {
                    postBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return SectorsPageTestHelper.CreateSectorSuccessResponse(newId);
                }

                return SectorsPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderSectorsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Perimeter");
        });

        cut.Find("button[aria-label='Adicionar setor']").Click();
        cut.Find("#sector-name-input").Input("Lobby");
        cut.Find("#sector-desc-input").Change("Main entrance");
        cut.Find("#sector-positions-input").Change("3");
        cut.Find("form button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            postBody.Should().NotBeNullOrEmpty();
            using var doc = JsonDocument.Parse(postBody!);
            doc.RootElement.GetProperty("name").GetString().Should().Be("Lobby");
            doc.RootElement.GetProperty("description").GetString().Should().Be("Main entrance");
            doc.RootElement.GetProperty("requiredGuardsPerDay").GetInt32().Should().Be(3);
            cut.Markup.Should().Contain("Lobby");
        });

        listCallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void AuthenticatedUser_WithEmptyList_ShowsEmptyStateMessage()
    {
        SectorsPageTestHelper.Register(
            Services,
            request => SectorsPageTestHelper.IsSectorsListGet(request)
                ? SectorsPageTestHelper.EmptyListResponse()
                : SectorsPageTestHelper.NotFoundResponse(),
            roles: UserRole.Supervisor);

        var cut = RenderSectorsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("p.muted[role='status']").TextContent
                .Should()
                .Contain("Não há setores encontrados para este filtro.");
            cut.Markup.Should().NotContain("Perimeter");
        });
    }

    private IRenderedComponent<CascadingAuthenticationState> RenderSectorsPage() =>
        RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Sectors>());
}
