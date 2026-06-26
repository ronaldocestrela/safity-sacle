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

public class PlatformPlansIntegrationTests
{
    private const string PlatformOwnerEmail = "platform.owner@test.local";
    private const string PlatformOwnerPassword = "Platform@12345";

    [Fact]
    public async Task PlatformPlans_List_AsPlatformOwner_ReturnsOk()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var response = await client.GetAsync("/api/platform/plans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformPlans_Create_AsPlatformOwner_ReturnsCreated()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var code = $"starter-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/platform/plans", new
        {
            name = "Starter",
            code,
            description = "Plano inicial",
            priceMonthly = 99.90m,
            maxSecurityGuards = 10,
            maxSectors = 5,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlatformPlans_Create_WithDuplicateCode_ReturnsConflict()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var code = $"dup-{Guid.NewGuid():N}"[..15];
        var payload = new
        {
            name = "Starter",
            code,
            description = "Plano inicial",
            priceMonthly = 99.90m,
            maxSecurityGuards = 10,
            maxSectors = 5,
        };

        var first = await client.PostAsJsonAsync("/api/platform/plans", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/platform/plans", payload with { name = "Starter 2" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PlatformTenants_Create_WithActivePlan_ReturnsCreated()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var planId = await CreatePlanAsync(client);

        var email = $"tenant.plan.{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantName = "Tenant With Plan",
            adminName = "Plan Admin",
            adminEmail = email,
            adminPassword = "Created@12345",
            platformPlanId = planId,
            leadStatus = 2,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PlatformTenants_Create_ContractedWithoutPlan_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var email = $"tenant.contracted.{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantName = "Tenant Contracted",
            adminName = "Contracted Admin",
            adminEmail = email,
            adminPassword = "Created@12345",
            leadStatus = 3,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlatformTenants_UpdateCommercial_WithActivePlan_ReturnsNoContent()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var planId = await CreatePlanAsync(client);
        var tenantId = await CreateTenantAsync(client);
        var response = await client.PatchAsJsonAsync(
            $"/api/platform/tenants/{tenantId}/commercial",
            new
            {
                platformPlanId = planId,
                leadStatus = 3,
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PlatformTenants_UpdateCommercial_WithInactivePlan_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var planId = await CreatePlanAsync(client);
        var deactivate = await client.PatchAsync($"/api/platform/plans/{planId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tenantId = await CreateTenantAsync(client);
        var response = await client.PatchAsJsonAsync(
            $"/api/platform/tenants/{tenantId}/commercial",
            new
            {
                platformPlanId = planId,
                leadStatus = 2,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlatformTenants_UpdateCommercial_DowngradeBelowUsage_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(client, PlatformOwnerEmail, PlatformOwnerPassword);

        var largePlanId = await CreatePlanAsync(client, maxSecurityGuards: 10, maxSectors: 10);
        var smallPlanId = await CreatePlanAsync(client, maxSecurityGuards: 1, maxSectors: 1);
        var tenantId = await CreateTenantAsync(client, largePlanId);
        await SeedExtraSectorAsync(factory, tenantId);

        var response = await client.PatchAsJsonAsync(
            $"/api/platform/tenants/{tenantId}/commercial",
            new
            {
                platformPlanId = smallPlanId,
                leadStatus = 3,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task SeedExtraSectorAsync(TestWebApplicationFactory factory, Guid tenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafetyScale.Infrastructure.Persistence.ApplicationDbContext>();
        db.Sectors.Add(new SafetyScale.Domain.Entities.Sector
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Extra Sector",
            RequiredGuardsPerDay = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> CreatePlanAsync(
        HttpClient client,
        int maxSecurityGuards = 10,
        int maxSectors = 5)
    {
        var code = $"plan-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/platform/plans", new
        {
            name = "Business",
            code,
            description = "Plano business",
            priceMonthly = 199.90m,
            maxSecurityGuards,
            maxSectors,
        });

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateTenantAsync(HttpClient client, Guid? platformPlanId = null)
    {
        var email = $"tenant.commercial.{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantName = "Commercial Tenant",
            adminName = "Commercial Admin",
            adminEmail = email,
            adminPassword = "Created@12345",
            platformPlanId,
            leadStatus = platformPlanId.HasValue ? 3 : 0,
        });

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("tenantId").GetGuid();
    }
}

internal static class PlatformTestHelper
{
    private const string PlatformOwnerEmail = "platform.owner@test.local";
    private const string PlatformOwnerPassword = "Platform@12345";
    private const string PlatformSupportEmail = "platform.support@test.local";
    private const string PlatformSupportPassword = "Support@12345";

    public static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    public static async Task AuthenticateAsPlatformUserAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/platform/login", new { email, password });
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Authentication response did not include JWT token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task EnsurePlatformUsersAsync(TestWebApplicationFactory factory)
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

internal static class HttpClientJsonExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value) =>
        client.PatchAsync(requestUri, JsonContent.Create(value));
}
