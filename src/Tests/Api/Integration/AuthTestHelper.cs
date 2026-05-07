using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SafetyScale.Tests.Api.Integration;

public static class AuthTestHelper
{
    public static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@safetyscale.local",
            password = "Admin@12345"
        });

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Authentication response did not include JWT token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task AuthenticateAsSupervisorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "supervisor@safetyscale.local",
            password = "Supervisor@12345"
        });

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Authentication response did not include JWT token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
