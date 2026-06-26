using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Tests.Api.Integration;

public sealed class SecurityGuardInviteEndpointsTests
{
    [Fact]
    public async Task Post_ShouldCreateGuardUserAndEnqueueInviteEmail()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        const string email = "invited.guard@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/security-guards",
            new { name = "Invited Guard", email });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        payload.Should().NotBeNull();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeTrue();
        user.SecurityGuardId.Should().Be(payload!.Id);
        (await userManager.IsInRoleAsync(user, IdentitySeeder.Roles.SecurityGuard)).Should().BeTrue();

        var messages = await db.EmailQueueMessages
            .Where(m => m.To == email)
            .ToListAsync();
        messages.Should().ContainSingle(m =>
            m.Subject.Contains("Defina sua senha", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SetPassword_ShouldAllowInvitedGuardToLogin()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        const string email = "login.guard@example.com";
        const string password = "Guard@12345";
        var createResponse = await client.PostAsJsonAsync(
            "/api/security-guards",
            new { name = "Login Guard", email });
        createResponse.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();

        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        client.DefaultRequestHeaders.Authorization = null;

        var setPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/set-password",
            new { userId = user!.Id, token, password });

        setPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        await using var stream = await loginResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var jwt = document.RootElement.GetProperty("token").GetString();
        jwt.Should().NotBeNullOrWhiteSpace();

        var payload = JwtTestHelper.ParsePayload(jwt!);
        payload.TryGetProperty("security_guard_id", out var guardClaim).Should().BeTrue();
        guardClaim.GetString().Should().NotBeNullOrWhiteSpace();
        JwtTestHelper.CollectRoles(payload).Should().Contain(IdentitySeeder.Roles.SecurityGuard);
    }

    [Fact]
    public async Task SecurityGuard_ShouldOnlySeeOwnScheduleItems()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardAId = await CreateGuardAsync(client, "Scope Guard A", "scope.a@example.com");
        var guardBId = await CreateGuardAsync(client, "Scope Guard B", "scope.b@example.com");

        await client.PostAsJsonAsync("/api/sectors", new { name = "Scope Sector", description = (string?)null });

        await client.PutAsJsonAsync(
            $"/api/security-guards/{guardAId}/sectors",
            new { sectorIds = Array.Empty<Guid>() });
        await client.PutAsJsonAsync(
            $"/api/security-guards/{guardBId}/sectors",
            new { sectorIds = Array.Empty<Guid>() });

        var generate = await client.PostAsJsonAsync("/api/schedules/generate", new { month = 8, year = 2035 });
        generate.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var guardAUser = await userManager.FindByEmailAsync("scope.a@example.com");
        guardAUser.Should().NotBeNull();

        var token = await userManager.GeneratePasswordResetTokenAsync(guardAUser!);
        client.DefaultRequestHeaders.Authorization = null;
        await client.PostAsJsonAsync(
            "/api/auth/set-password",
            new { userId = guardAUser!.Id, token, password = "Guard@12345" });

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "scope.a@example.com", password = "Guard@12345" });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var scheduleResponse = await client.GetAsync("/api/schedules/month/8/year/2035");
        scheduleResponse.EnsureSuccessStatusCode();
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<MonthlySchedulePayload>();
        schedule.Should().NotBeNull();
        schedule!.Items.Should().NotBeEmpty();
        schedule.Items.Should().OnlyContain(i => i.SecurityGuardId == guardAId);
        schedule.Items.Should().NotContain(i => i.SecurityGuardId == guardBId);
    }

    private static async Task<Guid> CreateGuardAsync(HttpClient client, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/api/security-guards", new { name, email });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        return payload!.Id;
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private sealed record CreateSecurityGuardResponse(Guid Id);

    private sealed record LoginResponse(string Token);

    private sealed record MonthlySchedulePayload(
        Guid Id,
        int Month,
        int Year,
        DateTime GeneratedAt,
        List<ScheduleItemPayload> Items);

    private sealed record ScheduleItemPayload(
        Guid Id,
        Guid SecurityGuardId,
        string SecurityGuardName,
        bool SecurityGuardIsActive,
        Guid SectorId,
        string SectorName,
        DateOnly Date,
        bool IsWeekend);
}
