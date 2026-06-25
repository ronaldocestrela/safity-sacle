using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Tests.Api.Integration;

public class MultiTenantIsolationIntegrationTests
{
    private static readonly Guid SecondTenantId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Tenant2_ListGuards_DoesNotInclude_Tenant1_Guard()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedSecondTenantAdminAsync(factory);

        using var clientT1 = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(clientT1);
        var created = await clientT1.PostAsJsonAsync("/api/security-guards", new { name = "T1 Guard" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        createdBody.Should().NotBeNull();
        var idT1 = createdBody!.Id;

        using var clientT2 = CreateHttpsClient(factory);
        await AuthenticateAsSecondTenantAdminAsync(clientT2);

        var listResp = await clientT2.GetAsync("/api/security-guards");
        listResp.EnsureSuccessStatusCode();
        var items = await listResp.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().NotBeNull();
        items!.Should().NotContain(x => x.Id == idT1);
    }

    [Fact]
    public async Task Tenant2_ListSectors_DoesNotInclude_Tenant1_Sector()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedSecondTenantAdminAsync(factory);

        using var clientT1 = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(clientT1);
        await clientT1.PostAsJsonAsync("/api/sectors", new { name = "T1OnlySector", description = (string?)null });

        using var clientT2 = CreateHttpsClient(factory);
        await AuthenticateAsSecondTenantAdminAsync(clientT2);

        var listResp = await clientT2.GetAsync("/api/sectors");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await listResp.Content.ReadFromJsonAsync<List<SectorResponse>>();
        items.Should().NotBeNull();
        items!.Should().NotContain(x => x.Name == "T1OnlySector");
    }

    [Fact]
    public async Task Tenant2_GetScheduleById_FromTenant1_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedSecondTenantAdminAsync(factory);

        using var clientT1 = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(clientT1);

        await clientT1.PostAsJsonAsync("/api/security-guards", new { name = "T1 Gen A" });
        await clientT1.PostAsJsonAsync("/api/security-guards", new { name = "T1 Gen B" });

        var gen = await clientT1.PostAsJsonAsync("/api/schedules/generate", new { month = 4, year = 2055 });
        gen.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await gen.Content.ReadFromJsonAsync<ScheduleCreatedResponse>();
        body.Should().NotBeNull();
        var schedId = body!.Id;

        using var clientT2 = CreateHttpsClient(factory);
        await AuthenticateAsSecondTenantAdminAsync(clientT2);

        var getResp = await clientT2.GetAsync($"/api/schedules/{schedId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task AuthenticateAsSecondTenantAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "tenant2.admin@test.local",
            password = "Aa!23456x"
        });

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Authentication response did not include JWT token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task SeedSecondTenantAdminAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == SecondTenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = SecondTenantId,
                Name = "Second",
                Slug = "second-test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        const string email = "tenant2.admin@test.local";

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new AppUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            UserKind = UserKind.Tenant,
            TenantId = SecondTenantId,
        };

        var create = await userManager.CreateAsync(user, "Aa!23456x");
        create.Succeeded.Should().BeTrue($"errors: {string.Join(',', create.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, IdentitySeeder.Roles.Admin);
    }

    private sealed record CreateSecurityGuardResponse(Guid Id);

    private sealed record SectorResponse(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);

    private sealed record SecurityGuardResponse(
        Guid Id,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        List<SectorResponse>? Sectors);

    private sealed record ScheduleCreatedResponse(Guid Id);
}
