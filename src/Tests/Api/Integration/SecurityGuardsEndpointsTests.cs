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

        var response = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard A" });

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
        await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard B" });

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

        var createResponse = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard C" });
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

        var createResponse = await client.PostAsJsonAsync("/api/security-guards", new { name = "Guard D" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSecurityGuardResponse>();
        created.Should().NotBeNull();

        var patchResponse = await client.PatchAsync($"/api/security-guards/{created!.Id}/inactive", content: null);

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/security-guards");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SecurityGuardResponse>>();
        items.Should().Contain(x => x.Id == created.Id && !x.IsActive);
    }

    private sealed record CreateSecurityGuardResponse(Guid Id);
    private sealed record SecurityGuardResponse(Guid Id, string Name, bool IsActive, DateTime CreatedAt);

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
}
