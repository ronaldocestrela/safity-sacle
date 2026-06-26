using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SafetyScale.Api.Contracts.Schedules;

namespace SafetyScale.Tests.Api.Integration;

public class SchedulesGenerateEndpointsTests
{
    [Fact]
    public async Task Post_ShouldCreateSchedule_WhenAdminAndGuardsExist()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await CreateGuardAsync(client, "Schedule Guard One");
        await CreateGuardAsync(client, "Schedule Guard Two");

        var response = await client.PostAsJsonAsync("/api/schedules/generate", new { month = 4, year = 2038 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<IdResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(payload.Id.ToString());
    }

    [Fact]
    public async Task Post_ShouldConflict_WhenSameMonthYearGeneratedTwice()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await CreateGuardAsync(client, "Dup A");
        await CreateGuardAsync(client, "Dup B");

        var body = new { month = 5, year = 2039 };
        var first = await client.PostAsJsonAsync("/api/schedules/generate", body);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/schedules/generate", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_ShouldReturnForbidden_WhenSupervisor()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsSupervisorAsync(client);

        var response = await client.PostAsJsonAsync("/api/schedules/generate", new { month = 6, year = 2040 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldReturnBadRequest_WhenNoActiveGuards()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/schedules/generate", new { month = 7, year = 2041 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturnCoverageFailurePayload_WhenNoEligibleGuardForSectorWorkload()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        // Active sector alphabetically before "Primary", with staffing but no guard links ⇒ first day fails.
        var heavy = await client.PostAsJsonAsync(
            "/api/sectors",
            new { name = "HeavyUnstaffed", description = (string?)null, requiredGuardsPerDay = 1 });
        heavy.EnsureSuccessStatusCode();

        await CreateGuardAsync(client, "Cov Guard A");
        await CreateGuardAsync(client, "Cov Guard B");

        var response = await client.PostAsJsonAsync("/api/schedules/generate", new { month = 5, year = 2090 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ScheduleCoverageFailureResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.Code.Should().Be("ScheduleCoverageFailed");
        payload.FailedDate.Should().NotBeNull();
        payload.Message.Should().Contain(payload.FailedDate!.Value.ToString("dd/MM/yyyy"));

        payload.Message.Should().Contain("não há seguranças elegíveis suficientes");
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost"),
        });

    private static async Task<Guid> CreateGuardAsync(HttpClient client, string name)
    {
        var email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com";
        var response = await client.PostAsJsonAsync("/api/security-guards", new { name, email });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<IdResponse>();
        payload.Should().NotBeNull();
        return payload!.Id;
    }

    private sealed record IdResponse(Guid Id);
}
