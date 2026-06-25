using System.Net.Http.Headers;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class BearerTokenHandler(
    JwtSessionStorage tenantSessionStorage,
    PlatformJwtSessionStorage platformSessionStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var skipBearer = request.Options.TryGetValue(ApiHttpContext.SkipBearerInjectionKey, out var skip) && skip;
        var usePlatformToken = IsPlatformRequest(request);
        var token = usePlatformToken
            ? await platformSessionStorage.GetStoredTokenAsync(cancellationToken)
            : await tenantSessionStorage.GetStoredTokenAsync(cancellationToken);
        var hadToken = !string.IsNullOrEmpty(token);

        request.Options.Set(ApiHttpContext.HadTokenKey, hadToken);
        request.Options.Set(ApiHttpContext.UsesPlatformTokenKey, usePlatformToken);

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

    private static bool IsPlatformRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        return path.Contains("/api/platform/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/api/auth/platform/", StringComparison.OrdinalIgnoreCase);
    }
}
