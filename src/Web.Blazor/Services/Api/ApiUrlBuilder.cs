namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Builds API request URLs from <see cref="AppConfiguration.ApiBaseUrl"/>.
/// Parity with React <c>buildApiUrl</c> / <c>apiUrl</c> in <c>src/Web/src/shared/config/env.ts</c>.
/// </summary>
public sealed class ApiUrlBuilder(AppConfiguration configuration)
{
    /// <summary>
    /// Returns an absolute URL when <see cref="AppConfiguration.ApiBaseUrl"/> is set;
    /// otherwise a root-relative path (leading slash), e.g. <c>/api/health</c>.
    /// </summary>
    public string Build(string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return string.IsNullOrEmpty(configuration.ApiBaseUrl)
            ? normalizedPath
            : $"{configuration.ApiBaseUrl}{normalizedPath}";
    }
}
