using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SafetyScale.Tests.Api.Integration;

public class SecurityGuardsEndpointsTests
{
    [Fact]
    public async Task Post_ShouldCreateSecurityGuard()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard A", email = "guard-a@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Get_ShouldReturnCreatedSecurityGuards()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard B", email = "guard-b@example.com" });

        var response = await client.GetAsync("/api/security-guards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().NotBeNull();
        items!.Should().Contain(x => x.Name == "Guard B" && x.IsActive);
    }

    [Fact]
    public async Task Put_ShouldUpdateSecurityGuardName()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard C", email = "guard-c@example.com" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        created.Should().NotBeNull();

        var updateResponse = await client.PutAsJsonAsync($"/api/security-guards/{created!.Id}", new { name = "Guard C Updated" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/security-guards");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().Contain(x => x.Id == created.Id && x.Name == "Guard C Updated");
    }

    [Fact]
    public async Task PatchInactive_ShouldInactivateSecurityGuard()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard D", email = "guard-d@example.com" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        created.Should().NotBeNull();

        var patchResponse = await client.PatchAsync($"/api/security-guards/{created!.Id}/inactive", content: null);

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/security-guards");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().Contain(x => x.Id == created.Id && !x.IsActive);
    }

    [Fact]
    public async Task PatchActive_ShouldActivateSecurityGuard()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard E", email = "guard-e@example.com" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        created.Should().NotBeNull();

        await client.PatchAsync($"/api/security-guards/{created!.Id}/inactive", content: null);

        var patchResponse = await client.PatchAsync($"/api/security-guards/{created.Id}/active", content: null);

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/security-guards");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().Contain(x => x.Id == created.Id && x.IsActive);
    }

    [Fact]
    public async Task PutSectors_ShouldAssignActiveSectors()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var sectorResp = await client.PostAsJsonAsync("/api/sectors", new { name = "Gate A", description = (string?)null });
        sectorResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sector = await sectorResp.Content.ReadFromJsonAsync<SectorStubResponse>();
        sector.Should().NotBeNull();

        var createResp = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard Sector", email = "guard-sector@example.com" });
        var createdGuard = await createResp.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        createdGuard.Should().NotBeNull();

        var put = await client.PutAsJsonAsync(
            $"/api/security-guards/{createdGuard!.Id}/sectors",
            new { sectorIds = new[] { sector!.Id } });

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/security-guards");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().NotBeNull();
        var g = items!.Single(x => x.Id == createdGuard.Id);
        g.Sectors.Should().NotBeNull();
        g.Sectors!.Should().Contain(s => s.Id == sector.Id && s.Name == "Gate A");
    }

    private sealed record CreateSecurityGuardResponse(Guid Id);
    private sealed record SectorStubResponse(Guid Id);
    private sealed record GuardSectorResponse(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
    private sealed record SecurityGuardResponse(
        Guid Id,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        List<GuardSectorResponse>? Sectors);

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
}
