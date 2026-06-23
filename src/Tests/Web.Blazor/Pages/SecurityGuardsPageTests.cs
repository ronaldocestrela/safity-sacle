using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Pages.App;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class SecurityGuardsPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Supervisor_WithLoadedList_HidesWriteControls()
    {
        SecurityGuardsPageTestHelper.Register(
            Services,
            SecurityGuardsPageTestHelper.DefaultHandler,
            roles: UserRole.Supervisor);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("section[aria-label='Personnel list']").TextContent.Should().Contain("Ana Costa");
            cut.FindAll("button.fab").Should().BeEmpty();
            cut.FindAll("button.name-btn").Should().BeEmpty();
            cut.Find("button[role='switch'][aria-label='Active status for Ana Costa']")
                .HasAttribute("disabled")
                .Should()
                .BeTrue();
        });
    }

    [Fact]
    public void Admin_WithLoadedList_ShowsWriteControls()
    {
        SecurityGuardsPageTestHelper.Register(
            Services,
            SecurityGuardsPageTestHelper.DefaultHandler,
            roles: UserRole.Admin);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("button[aria-label='Adicionar segurança']").Should().NotBeNull();
            cut.Find("button.name-btn").TextContent.Should().Contain("Ana Costa");
        });
    }

    [Fact]
    public void AuthenticatedUser_WithEmptyList_ShowsEmptyStateMessage()
    {
        SecurityGuardsPageTestHelper.Register(
            Services,
            request =>
            {
                if (SecurityGuardsPageTestHelper.IsGuardsListGet(request))
                {
                    return SecurityGuardsPageTestHelper.EmptyGuardsResponse();
                }

                if (SecurityGuardsPageTestHelper.IsSectorsListGet(request))
                {
                    return SecurityGuardsPageTestHelper.DefaultSectorsResponse();
                }

                return SecurityGuardsPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Supervisor);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("p.muted[role='status']").TextContent
                .Should()
                .Contain("Não há seguranças encontradas para este filtro.");
        });
    }

    [Fact]
    public void Admin_WithForbiddenList_ShowsAlertMessage()
    {
        SecurityGuardsPageTestHelper.Register(
            Services,
            request =>
            {
                if (SecurityGuardsPageTestHelper.IsGuardsListGet(request))
                {
                    return SecurityGuardsPageTestHelper.ForbiddenResponse("Sem permissão na API.");
                }

                if (SecurityGuardsPageTestHelper.IsSectorsListGet(request))
                {
                    return SecurityGuardsPageTestHelper.DefaultSectorsResponse();
                }

                return SecurityGuardsPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("Sem permissão na API.");
        });
    }

    [Fact]
    public void Admin_SubmitCreateWithoutName_ShowsValidationError()
    {
        SecurityGuardsPageTestHelper.Register(
            Services,
            SecurityGuardsPageTestHelper.DefaultHandler,
            roles: UserRole.Admin);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ana Costa"));

        cut.Find("button[aria-label='Adicionar segurança']").Click();
        cut.Find("form button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Enter a name.");
        });
    }

    [Fact]
    public void Admin_SubmitCreate_AssignsSectorsAndRefreshesList()
    {
        var listCallCount = 0;
        string? createBody = null;
        string? sectorsBody = null;
        var newId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        SecurityGuardsPageTestHelper.Register(
            Services,
            request =>
            {
                if (SecurityGuardsPageTestHelper.IsGuardsListGet(request))
                {
                    listCallCount++;
                    return listCallCount == 1
                        ? SecurityGuardsPageTestHelper.DefaultGuardsResponse()
                        : SecurityGuardsPageTestHelper.JsonResponse(SecurityGuardsPageTestHelper.MariaAndAnaJson);
                }

                if (SecurityGuardsPageTestHelper.IsSectorsListGet(request))
                {
                    return SecurityGuardsPageTestHelper.DefaultSectorsResponse();
                }

                if (SecurityGuardsPageTestHelper.IsGuardsCreatePost(request))
                {
                    createBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return SecurityGuardsPageTestHelper.CreateGuardSuccessResponse(newId);
                }

                if (SecurityGuardsPageTestHelper.IsGuardsSetSectorsPut(request))
                {
                    sectorsBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                    return SecurityGuardsPageTestHelper.NoContentResponse();
                }

                return SecurityGuardsPageTestHelper.NotFoundResponse();
            },
            roles: UserRole.Admin);

        var cut = RenderGuardsPage();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ana Costa"));

        cut.Find("button[aria-label='Adicionar segurança']").Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Sector A"));

        cut.Find("input[type='checkbox']").Change(true);
        cut.Find("#sg-name-input").Input("Maria Souza");
        cut.Find("form button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            createBody.Should().NotBeNullOrEmpty();
            using var createDoc = JsonDocument.Parse(createBody!);
            createDoc.RootElement.GetProperty("name").GetString().Should().Be("Maria Souza");

            sectorsBody.Should().NotBeNullOrEmpty();
            using var sectorsDoc = JsonDocument.Parse(sectorsBody!);
            sectorsDoc.RootElement.GetProperty("sectorIds").GetArrayLength().Should().Be(1);
            sectorsDoc.RootElement.GetProperty("sectorIds")[0].GetString()
                .Should()
                .Be("11111111-1111-1111-1111-111111111111");

            cut.Markup.Should().Contain("Maria Souza");
        });

        listCallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private IRenderedComponent<CascadingAuthenticationState> RenderGuardsPage() =>
        RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<SecurityGuards>());
}
