namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Per-request HTTP context keys shared by <see cref="BearerTokenHandler"/> and
/// <see cref="UnauthorizedRedirectHandler"/>.
/// </summary>
internal static class ApiHttpContext
{
    public static readonly HttpRequestOptionsKey<bool> SkipAuthRedirectKey =
        new("SafetyScale.SkipAuthRedirect");

    public static readonly HttpRequestOptionsKey<bool> SkipBearerInjectionKey =
        new("SafetyScale.SkipBearerInjection");

    public static readonly HttpRequestOptionsKey<bool> HadTokenKey =
        new("SafetyScale.HadToken");

    public static readonly HttpRequestOptionsKey<bool> UsesPlatformTokenKey =
        new("SafetyScale.UsesPlatformToken");
}
