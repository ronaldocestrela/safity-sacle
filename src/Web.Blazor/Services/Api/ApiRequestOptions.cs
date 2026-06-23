namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Options for a single API request. Parity with React <c>skipAuthRedirect</c> on <c>apiFetch</c>.
/// </summary>
public sealed class ApiRequestOptions
{
    public static ApiRequestOptions Default { get; } = new();

    /// <summary>
    /// When true, a 401 does not clear session or navigate to <c>/login</c> (e.g. login form).
    /// </summary>
    public bool SkipAuthRedirect { get; init; }

    /// <summary>
    /// When true, stored JWT is not injected as Bearer (POC: health without token).
    /// </summary>
    public bool SkipBearerInjection { get; init; }
}
