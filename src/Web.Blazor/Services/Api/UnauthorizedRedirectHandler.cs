using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class UnauthorizedRedirectHandler(
    JwtSessionStorage tenantSessionStorage,
    PlatformJwtSessionStorage platformSessionStorage,
    CustomAuthStateProvider authStateProvider,
    NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var skipRedirect = request.Options.TryGetValue(ApiHttpContext.SkipAuthRedirectKey, out var skip) && skip;
        var hadToken = request.Options.TryGetValue(ApiHttpContext.HadTokenKey, out var had) && had;
        var usesPlatformToken = request.Options.TryGetValue(ApiHttpContext.UsesPlatformTokenKey, out var platform) &&
                                platform;

        if (!skipRedirect && hadToken)
        {
            if (usesPlatformToken)
            {
                await platformSessionStorage.ClearAsync(cancellationToken);
            }
            else
            {
                await tenantSessionStorage.ClearAsync(cancellationToken);
            }

            authStateProvider.NotifyAuthenticationStateChanged();

            if (usesPlatformToken)
            {
                navigationManager.NavigateTo("/platform/login?reason=session-expired", replace: true);
            }
            else
            {
                navigationManager.NavigateTo("/login?reason=session-expired", replace: true);
            }
        }

        return response;
    }
}
