using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Tests.Api.Integration;

public class PlatformAuthIntegrationTests
{
    private const string PlatformOwnerEmail = "platform.owner@test.local";
    private const string PlatformOwnerPassword = "Platform@12345";
    private const string PlatformSupportEmail = "platform.support@test.local";
    private const string PlatformSupportPassword = "Support@12345";

    [Fact]
    public async Task PlatformLogin_WithPlatformOwner_ReturnsToken()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync("/api/auth/platform/login", new
        {
            email = PlatformOwnerEmail,
            password = PlatformOwnerPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        document.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TenantLogin_WithPlatformOwner_ReturnsUnauthorized()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = PlatformOwnerEmail,
            password = PlatformOwnerPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlatformTenants_List_AsPlatformOwner_ReturnsOk()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);
        await AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var response = await client.GetAsync("/api/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformTenants_List_AsTenantAdmin_ReturnsForbidden()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/platform/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformTenants_Create_AsPlatformSupport_ReturnsForbidden()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);
        await AuthenticateAsPlatformUserAsync(client, PlatformSupportEmail, PlatformSupportPassword);

        var response = await client.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantName = "Acme Corp",
            adminName = "Acme Admin",
            adminEmail = "acme.admin@test.local",
            adminPassword = "Acme@12345",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformTenants_Create_AsPlatformOwner_ReturnsCreated()
    {
        using var factory = new TestWebApplicationFactory();
        await EnsurePlatformUsersAsync(factory);
        using var client = CreateHttpsClient(factory);
        await AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var email = $"tenant.created.{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantName = "Created From Platform",
            adminName = "Created Admin",
            adminEmail = email,
            adminPassword = "Created@12345",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    private static async Task AuthenticateAsPlatformUserAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/platform/login", new { email, password });
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Authentication response did not include JWT token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task EnsurePlatformUsersAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in IdentitySeeder.PlatformRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsurePlatformUserAsync(
            userManager,
            PlatformOwnerEmail,
            PlatformOwnerPassword,
            IdentitySeeder.PlatformRoles.Owner);

        await EnsurePlatformUserAsync(
            userManager,
            PlatformSupportEmail,
            PlatformSupportPassword,
            IdentitySeeder.PlatformRoles.Support);
    }

    private static async Task EnsurePlatformUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is null)
        {
            existing = new AppUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                UserKind = UserKind.Platform,
                TenantId = null,
                DisplayName = email.Split('@')[0],
            };

            var create = await userManager.CreateAsync(existing, password);
            create.Succeeded.Should().BeTrue(string.Join(',', create.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(existing, role))
        {
            await userManager.AddToRoleAsync(existing, role);
        }
    }
}
