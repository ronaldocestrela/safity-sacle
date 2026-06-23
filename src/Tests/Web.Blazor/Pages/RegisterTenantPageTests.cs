using Bunit;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Pages.Auth;

namespace SafetyScale.Tests.Web.Blazor.Pages;

public sealed class RegisterTenantPageTests : BlazorComponentTestBase
{
    [Fact]
    public void Submit_WithEmailConflict_ShowsFriendlyMessage()
    {
        PublicAuthTestHelper.Register(
            Services,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                return path.Contains("/api/tenants/register", StringComparison.Ordinal)
                    ? PublicAuthTestHelper.SignupEmailConflictResponse()
                    : PublicAuthTestHelper.NotFoundResponse();
            },
            initialUri: "/signup");

        var cut = RenderComponent<RegisterTenant>();

        cut.Find("input[name='tenantName']").Input("Empresa Beta");
        cut.Find("input[name='adminName']").Input("Maria");
        cut.Find("input[name='adminEmail']").Input("dup@test.local");
        cut.Find("input[name='adminPassword']").Input("Aa!23456z");
        cut.Find("input[name='confirmPassword']").Input("Aa!23456z");
        cut.Find("button.submit").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Be("Este e-mail já está cadastrado.");
        });
    }
}
