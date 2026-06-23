using System.Text;
using System.Text.Json;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Port of React <c>jwtTestUtils.ts</c> for Blazor JWT/session tests.</summary>
public static class JwtTestUtils
{
    public const string DefaultTestTenantId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    public static string MakeUnsignedJwt(IReadOnlyDictionary<string, object?> payload)
    {
        var merged = new Dictionary<string, object?>(payload);
        merged.TryAdd("tenant_id", DefaultTestTenantId);

        var header = ToBase64Url("""{"alg":"none","typ":"JWT"}""");
        var body = ToBase64Url(JsonSerializer.Serialize(merged));
        return $"{header}.{body}.sig";
    }

    public static long ExpSoon(int secondsFromNow = 3600) =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() + secondsFromNow;

    private static string ToBase64Url(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
