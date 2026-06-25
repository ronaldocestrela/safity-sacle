using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Tests.Api.Integration;

namespace SafetyScale.Tests.Api.Integration;

public class TenantsRegistrationEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record RegisterTenantResponseDto(Guid TenantId, string AdminUserId, string TenantSlug);

    [Fact]
    public async Task PostRegister_ShouldCreateTenant_And_AdminGetsJwt_AfterEmailConfirmation()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        var email = $"signup-{Guid.NewGuid():N}@test.local";
        const string password = "Aa!23456z";

        var response = await client.PostAsJsonAsync(
            "/api/tenants/register",
            new
            {
                tenantName = $"Empresa Signup {Guid.NewGuid():N}",
                adminName = "Fulano Silva",
                adminEmail = email,
                adminPassword = password,
                confirmPassword = password,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<RegisterTenantResponseDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.TenantId.Should().NotBe(Guid.Empty);
        body.AdminUserId.Should().NotBeNullOrWhiteSpace();

        var loginBeforeConfirm = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        loginBeforeConfirm.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(body.AdminUserId);
        user.Should().NotBeNull();
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user!);

        var confirmResponse = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = user!.Id, token = confirmationToken });

        confirmResponse.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginDoc = await loginResp.Content.ReadFromJsonAsync<LoginEnvelope>(JsonOptions);
        loginDoc.Should().NotBeNull();
        var token = loginDoc!.Token!;
        ReadTenantIdFromJwt(token).Should().Be(body.TenantId.ToString());

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var guardsResp = await client.GetAsync("/api/security-guards");
        guardsResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostRegister_ShouldReturnConflict_WhenEmailAlreadyUsed()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        var email = $"dup-{Guid.NewGuid():N}@test.local";
        const string password = "Aa!23456z";
        var body = new
        {
            tenantName = $"Dup Co {Guid.NewGuid():N}",
            adminName = "Admin Dup",
            adminEmail = email,
            adminPassword = password,
            confirmPassword = password,
        };

        (await client.PostAsJsonAsync("/api/tenants/register", body)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/tenants/register", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostRegister_ShouldReturnBadRequest_WhenConfirmMismatch()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        var resp = await client.PostAsJsonAsync(
            "/api/tenants/register",
            new
            {
                tenantName = $"Mismatch Co {Guid.NewGuid():N}",
                adminName = "A",
                adminEmail = $"m-{Guid.NewGuid():N}@test.local",
                adminPassword = "Aa!23456z",
                confirmPassword = "Different!1Aa",
            });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_ShouldReturnBadRequest_WhenPasswordTooWeak()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        var resp = await client.PostAsJsonAsync(
            "/api/tenants/register",
            new
            {
                tenantName = $"WeakPwd Co {Guid.NewGuid():N}",
                adminName = "Weak",
                adminEmail = $"w-{Guid.NewGuid():N}@test.local",
                adminPassword = "short",
                confirmPassword = "short",
            });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record LoginEnvelope(string? Token);

    private static string? ReadTenantIdFromJwt(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
}
