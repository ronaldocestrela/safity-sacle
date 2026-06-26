using System.Text;
using System.Text.Json;
using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Web.Blazor.Services.Auth;

/// <summary>
/// Client-side JWT payload parsing (no signature validation). Parity with React <c>jwt.ts</c>.
/// </summary>
public static class JwtParser
{
    public const string TenantClaimKey = "tenant_id";
    public const string UserKindClaimKey = "user_kind";
    public const string SecurityGuardClaimKey = "security_guard_id";

    private static readonly string[] RoleClaimKeys =
    [
        "role",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    ];

    public static JsonElement? ParseJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var json = Base64UrlDecodeToString(parts[1]);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static AuthSession? BuildSessionFromToken(string token)
    {
        var payload = ParseJwtPayload(token);
        if (payload is null)
        {
            return null;
        }

        var root = payload.Value;
        if (IsJwtExpired(root))
        {
            return null;
        }

        if (IsPlatformUser(root))
        {
            return null;
        }

        var tenantId = TenantIdFromPayload(root);
        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        var roles = FilterAppRoles(CollectRoleClaims(root));
        var securityGuardId = SecurityGuardIdFromPayload(root);
        return new AuthSession(token, EmailFromPayload(root), roles, tenantId, securityGuardId);
    }

    public static PlatformAuthSession? BuildPlatformSessionFromToken(string token)
    {
        var payload = ParseJwtPayload(token);
        if (payload is null)
        {
            return null;
        }

        var root = payload.Value;
        if (IsJwtExpired(root))
        {
            return null;
        }

        if (!IsPlatformUser(root))
        {
            return null;
        }

        var roles = FilterPlatformRoles(CollectRoleClaims(root));
        return new PlatformAuthSession(token, EmailFromPayload(root), roles);
    }

    public static bool IsPlatformUser(JsonElement payload)
    {
        if (!payload.TryGetProperty(UserKindClaimKey, out var kind) ||
            kind.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return string.Equals(kind.GetString(), "Platform", StringComparison.Ordinal);
    }

    public static bool IsJwtExpired(JsonElement payload, DateTimeOffset? now = null)
    {
        if (!payload.TryGetProperty("exp", out var expElement))
        {
            return false;
        }

        if (expElement.ValueKind != JsonValueKind.Number || !expElement.TryGetInt64(out var expSeconds))
        {
            return false;
        }

        var instant = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
        return (now ?? DateTimeOffset.UtcNow) >= instant;
    }

    public static string? EmailFromPayload(JsonElement payload)
    {
        if (payload.TryGetProperty("email", out var email) &&
            email.ValueKind == JsonValueKind.String)
        {
            var text = email.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        if (payload.TryGetProperty("unique_name", out var uniqueName) &&
            uniqueName.ValueKind == JsonValueKind.String)
        {
            var text = uniqueName.GetString();
            if (!string.IsNullOrEmpty(text) && text.Contains('@'))
            {
                return text;
            }
        }

        return null;
    }

    public static string? TenantIdFromPayload(JsonElement payload)
    {
        if (payload.TryGetProperty(TenantClaimKey, out var tenant) &&
            tenant.ValueKind == JsonValueKind.String)
        {
            var text = tenant.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        return null;
    }

    public static Guid? SecurityGuardIdFromPayload(JsonElement payload)
    {
        if (payload.TryGetProperty(SecurityGuardClaimKey, out var guardId) &&
            guardId.ValueKind == JsonValueKind.String &&
            Guid.TryParse(guardId.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    internal static IReadOnlyList<string> CollectRoleClaims(JsonElement payload)
    {
        var roles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in RoleClaimKeys)
        {
            if (!payload.TryGetProperty(key, out var value))
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    var single = value.GetString();
                    if (!string.IsNullOrEmpty(single))
                    {
                        roles.Add(single);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in value.EnumerateArray())
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

        return roles.ToList();
    }

    internal static IReadOnlyList<UserRole> FilterAppRoles(IReadOnlyList<string> roles)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "Admin", "Supervisor", "SecurityGuard" };
        var result = new List<UserRole>();

        foreach (var role in roles)
        {
            if (!allowed.Contains(role))
            {
                continue;
            }

            if (Enum.TryParse<UserRole>(role, ignoreCase: false, out var parsed) &&
                !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    internal static IReadOnlyList<PlatformUserRole> FilterPlatformRoles(IReadOnlyList<string> roles)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "PlatformOwner",
            "PlatformAdmin",
            "PlatformSupport",
        };
        var result = new List<PlatformUserRole>();

        foreach (var role in roles)
        {
            if (!allowed.Contains(role))
            {
                continue;
            }

            if (Enum.TryParse<PlatformUserRole>(role, ignoreCase: false, out var parsed) &&
                !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static string Base64UrlDecodeToString(string segment)
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

        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }
}
