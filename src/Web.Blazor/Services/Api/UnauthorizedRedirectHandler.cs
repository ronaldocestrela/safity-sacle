using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Clears session and navigates to <c>/login</c> on 401 when a token existed before the request.
/// Parity with React <c>setOnUnauthorized</c> + <c>apiFetch</c> 401 handling.
/// </summary>
public sealed class UnauthorizedRedirectHandler(
    JwtSessionStorage sessionStorage,
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

        if (!skipRedirect && hadToken)
        {
            await sessionStorage.ClearAsync(cancellationToken);
            authStateProvider.NotifyAuthenticationStateChanged();
            navigationManager.NavigateTo("/login", replace: true);
        }

        return response;
    }
}
