using System.Net.Http.Headers;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Injects Bearer token from sessionStorage when present. Parity with React <c>apiFetch</c>.
/// </summary>
public sealed class BearerTokenHandler(JwtSessionStorage sessionStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var skipBearer = request.Options.TryGetValue(ApiHttpContext.SkipBearerInjectionKey, out var skip) && skip;
        var token = await sessionStorage.GetStoredTokenAsync(cancellationToken);
        var hadToken = !string.IsNullOrEmpty(token);

        request.Options.Set(ApiHttpContext.HadTokenKey, hadToken);

        if (!skipBearer &&
            !string.IsNullOrEmpty(token) &&
            request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (!request.Headers.Contains("Accept"))
        {
            request.Headers.Accept.ParseAdd("application/json");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
