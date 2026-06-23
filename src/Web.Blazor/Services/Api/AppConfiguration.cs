namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Application settings loaded from <c>wwwroot/appsettings*.json</c>.
/// Parity with React <c>VITE_API_BASE_URL</c> via <see cref="ApiBaseUrl"/>.
/// </summary>
public sealed class AppConfiguration
{
    internal const string ApiBaseUrlKey = "ApiBaseUrl";

    public AppConfiguration(IConfiguration configuration)
    {
        ApiBaseUrl = NormalizeApiBase(configuration[ApiBaseUrlKey]);
    }

    /// <summary>
    /// API origin without trailing slash. Empty means same-origin relative paths (e.g. <c>/api/health</c>).
    /// </summary>
    public string ApiBaseUrl { get; }

    /// <summary>
    /// Normalizes raw config: trim whitespace; empty/blank becomes <see cref="string.Empty"/>; strips trailing slash.
    /// Equivalent to React <c>normalizeApiBase</c> in <c>src/Web/src/shared/config/env.ts</c>.
    /// </summary>
    internal static string NormalizeApiBase(string? raw)
    {
        var t = raw?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return string.Empty;
        }

        return t.TrimEnd('/');
    }
}
