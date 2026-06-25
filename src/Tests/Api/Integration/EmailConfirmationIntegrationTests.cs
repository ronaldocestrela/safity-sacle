using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Tests.Api.Integration;

namespace SafetyScale.Tests.Api.Integration;

public class EmailConfirmationIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record RegisterTenantResponseDto(Guid TenantId, string AdminUserId, string TenantSlug);

    private sealed record LoginEnvelope(string? Token);

    private sealed record ErrorEnvelope(string? Message, string? Code);

    [Fact]
    public async Task Register_ShouldBlockLoginUntilEmailConfirmed()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        var email = $"confirm-{Guid.NewGuid():N}@test.local";
        const string password = "Aa!23456z";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/tenants/register",
            new
            {
                tenantName = $"Confirm Co {Guid.NewGuid():N}",
                adminName = "Confirm Admin",
                adminEmail = email,
                adminPassword = password,
                confirmPassword = password,
            });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<RegisterTenantResponseDto>(JsonOptions);
        registerBody.Should().NotBeNull();

        var loginBeforeConfirm = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        loginBeforeConfirm.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var loginError = await loginBeforeConfirm.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        loginError!.Code.Should().Be("email_not_confirmed");

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(registerBody!.AdminUserId);
        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeFalse();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResponse = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = user.Id, token });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAfterConfirm = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        loginAfterConfirm.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginAfterConfirm.Content.ReadFromJsonAsync<LoginEnvelope>(JsonOptions);
        loginBody!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnBadRequest_WhenTokenInvalid()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = Guid.NewGuid().ToString(), token = "invalid-token" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
}
