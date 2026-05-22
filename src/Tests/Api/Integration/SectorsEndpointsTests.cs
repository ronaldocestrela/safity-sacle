using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SafetyScale.Tests.Api.Integration;

public class SectorsEndpointsTests
{
    [Fact]
    public async Task Post_ShouldCreateSector()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/sectors", new { name = "North Wing", description = "Floors 1–3" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CreateSectorResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Get_ShouldReturnCreatedSectors()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await client.PostAsJsonAsync("/api/sectors", new { name = "Lobby", description = (string?)null });

        var response = await client.GetAsync("/api/sectors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<SectorResponse>>();
        items.Should().NotBeNull();
        items!.Should().Contain(x => x.Name == "Lobby" && x.IsActive);
    }

    [Fact]
    public async Task Put_ShouldUpdateSector()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/sectors", new { name = "Old", description = "d" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSectorResponse>();
        created.Should().NotBeNull();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/sectors/{created!.Id}",
            new { name = "New", description = "nd" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/sectors");
        var items = await listResponse.Content.ReadFromJsonAsync<List<SectorResponse>>();
        items.Should().Contain(x => x.Id == created.Id && x.Name == "New");
    }

    private sealed record CreateSectorResponse(Guid Id);
    private sealed record SectorResponse(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory)
        => factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
}
