using System.Text;
using System.Text.Json;

namespace SafetyScale.Tests.Api.Integration;

internal static class JwtTestHelper
{
    public static JsonElement ParsePayload(string token)
    {
        var parts = token.Split('.');
        var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static IReadOnlyList<string> CollectRoles(JsonElement payload)
    {
        var roles = new List<string>();
        if (payload.TryGetProperty("role", out var roleElement))
        {
            AddRoleValues(roleElement, roles);
        }

        const string roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        if (payload.TryGetProperty(roleClaim, out var legacyRoleElement))
        {
            AddRoleValues(legacyRoleElement, roles);
        }

        return roles;
    }

    private static void AddRoleValues(JsonElement element, List<string> roles)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var single = element.GetString();
                if (!string.IsNullOrEmpty(single))
                {
                    roles.Add(single);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var role = item.GetString();
                        if (!string.IsNullOrEmpty(role))
                        {
                            roles.Add(role);
                        }
                    }
                }

                break;
        }
    }

    private static byte[] Base64UrlDecode(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder == 2)
        {
            padded += "==";
        }
        else if (remainder == 3)
        {
            padded += "=";
        }

        return Convert.FromBase64String(padded);
    }
}
