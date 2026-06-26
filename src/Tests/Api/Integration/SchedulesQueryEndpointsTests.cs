using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SafetyScale.Tests.Api.Integration;

public class SchedulesQueryEndpointsTests
{
    [Fact]
    public async Task GetById_ShouldReturnSchedule_WithGuardDetails_WhenAdmin()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await CreateGuardAsync(client, "Query Guard One");
        await CreateGuardAsync(client, "Query Guard Two");

        var month = 4;
        var year = 2066;
        var created = await client.PostAsJsonAsync("/api/schedules/generate", new { month, year });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPayload = await created.Content.ReadFromJsonAsync<IdResponse>();
        createdPayload.Should().NotBeNull();
        var scheduleId = createdPayload!.Id;

        var response = await client.GetAsync($"/api/schedules/{scheduleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await response.Content.ReadFromJsonAsync<MonthlySchedulePayload>();
        schedule.Should().NotBeNull();
        schedule!.Month.Should().Be(month);
        schedule.Year.Should().Be(year);
        schedule.Items.Should().HaveCount(DateTime.DaysInMonth(year, month));
        schedule.Items.Should().AllSatisfy(i =>
        {
            i.SecurityGuardId.Should().NotBe(Guid.Empty);
            i.SecurityGuardName.Should().NotBeNullOrWhiteSpace();
            i.SectorId.Should().NotBe(Guid.Empty);
            i.SectorName.Should().NotBeNullOrWhiteSpace();
        });
        schedule.Items.Select(i => i.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetByMonthYear_ShouldReturnSameSchedule_AsGetById()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await CreateGuardAsync(client, "A");
        await CreateGuardAsync(client, "B");

        var month = 8;
        var year = 2067;
        var created = await client.PostAsJsonAsync("/api/schedules/generate", new { month, year });
        var createdPayload = await created.Content.ReadFromJsonAsync<IdResponse>();
        var scheduleId = createdPayload!.Id;

        var byId = await client.GetAsync($"/api/schedules/{scheduleId}");
        var byMonth = await client.GetAsync($"/api/schedules/month/{month}/year/{year}");

        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        byMonth.StatusCode.Should().Be(HttpStatusCode.OK);
        var a = await byId.Content.ReadFromJsonAsync<MonthlySchedulePayload>();
        var b = await byMonth.Content.ReadFromJsonAsync<MonthlySchedulePayload>();
        a.Should().NotBeNull();
        b.Should().NotBeNull();
        a!.Id.Should().Be(b!.Id);
        a.Items.Should().HaveCount(b.Items.Count);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenIdMissing()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync($"/api/schedules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByMonthYear_ShouldReturnNotFound_WhenNotGenerated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/schedules/month/2/year/2077");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldAllowSupervisor()
    {
        using var factory = new TestWebApplicationFactory();
        using var adminClient = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(adminClient);

        await CreateGuardAsync(adminClient, "S One");
        await CreateGuardAsync(adminClient, "S Two");
        var created = await adminClient.PostAsJsonAsync("/api/schedules/generate", new { month = 11, year = 2068 });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var supervisorClient = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsSupervisorAsync(supervisorClient);

        var byId = await supervisorClient.GetAsync($"/api/schedules/{id}");
        var byMonth = await supervisorClient.GetAsync("/api/schedules/month/11/year/2068");

        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        byMonth.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync($"/api/schedules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldPreserveGuardName_InHistory_AfterGuardInactivated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardOne = await CreateGuardAsync(client, "To Inactivate Later");
        await CreateGuardAsync(client, "Other Active");

        var month = 3;
        var year = 2070;
        var created = await client.PostAsJsonAsync("/api/schedules/generate", new { month, year });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using var deactivate = new HttpRequestMessage(HttpMethod.Patch, $"/api/security-guards/{guardOne}/inactive");
        var patch = await client.SendAsync(deactivate);
        patch.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var payload = await (await client.GetAsync($"/api/schedules/month/{month}/year/{year}"))
            .Content.ReadFromJsonAsync<MonthlySchedulePayload>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(i =>
            i.SecurityGuardId == guardOne &&
            i.SecurityGuardName == "To Inactivate Later" &&
            !i.SecurityGuardIsActive);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

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

    private sealed record MonthlySchedulePayload(
        Guid Id,
        int Month,
        int Year,
        DateTime GeneratedAt,
        IReadOnlyList<ScheduleItemPayload> Items);

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
