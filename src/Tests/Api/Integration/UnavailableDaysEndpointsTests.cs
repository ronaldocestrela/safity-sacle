using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SafetyScale.Tests.Api.Integration;

public class UnavailableDaysEndpointsTests
{
    [Fact]
    public async Task Post_ShouldCreateUnavailableDay()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardId = await CreateGuardAsync(client, "Unavailable Guard A");

        var response = await client.PostAsJsonAsync(
            $"/api/security-guards/{guardId}/unavailable-days",
            new { date = "2030-04-10", reason = "Test reason" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<IdResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(payload.Id.ToString());
    }

    [Fact]
    public async Task Get_ShouldListUnavailableDays()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardId = await CreateGuardAsync(client, "Unavailable Guard B");
        await client.PostAsJsonAsync(
            $"/api/security-guards/{guardId}/unavailable-days",
            new { date = "2030-05-15", reason = "List me" });

        var response = await client.GetAsync($"/api/security-guards/{guardId}/unavailable-days");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<UnavailableDayResponse>>();
        items.Should().NotBeNull();
        items!.Should().ContainSingle(x => x.SecurityGuardId == guardId && x.Date == DateOnly.Parse("2030-05-15"));
    }

    [Fact]
    public async Task Get_ShouldAllowSupervisor()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsSupervisorAsync(client);

        using var adminClient = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(adminClient);
        var guardId = await CreateGuardAsync(adminClient, "Supervisor Reads");

        await adminClient.PostAsJsonAsync(
            $"/api/security-guards/{guardId}/unavailable-days",
            new { date = "2030-06-01", reason = null as string });

        var response = await client.GetAsync($"/api/security-guards/{guardId}/unavailable-days");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenGuardDoesNotExist()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var missingId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/security-guards/{missingId}/unavailable-days");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldConflict_WhenDuplicateDateForSameGuard()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardId = await CreateGuardAsync(client, "Duplicate Day Guard");
        var payload = new { date = "2030-07-07", reason = "First" };

        var first = await client.PostAsJsonAsync($"/api/security-guards/{guardId}/unavailable-days", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync($"/api/security-guards/{guardId}/unavailable-days", payload);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_ShouldReturnBadRequest_WhenGuardInactive()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardId = await CreateGuardAsync(client, "Inactive Guard");
        var patch = await client.PatchAsync($"/api/security-guards/{guardId}/inactive", content: null);
        patch.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await client.PostAsJsonAsync(
            $"/api/security-guards/{guardId}/unavailable-days",
            new { date = "2030-08-08", reason = null as string });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldRemoveUnavailableDay()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var guardId = await CreateGuardAsync(client, "Delete Me Guard");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/security-guards/{guardId}/unavailable-days",
            new { date = "2030-09-09", reason = null as string });

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        created.Should().NotBeNull();

        var deleteResponse = await client.DeleteAsync($"/api/unavailable-days/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync($"/api/security-guards/{guardId}/unavailable-days");
        var items = await listResponse.Content.ReadFromJsonAsync<List<UnavailableDayResponse>>();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.DeleteAsync($"/api/unavailable-days/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost"),
        });

    private static async Task<Guid> CreateGuardAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/security-guards", new { name });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<IdResponse>();
        payload.Should().NotBeNull();
        return payload!.Id;
    }

    private sealed record IdResponse(Guid Id);

    private sealed record UnavailableDayResponse(Guid Id, Guid SecurityGuardId, DateOnly Date, string? Reason);
}
