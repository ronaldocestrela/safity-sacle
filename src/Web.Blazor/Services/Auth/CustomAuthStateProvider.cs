using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Web.Blazor.Services.Auth;

/// <summary>
/// Authentication state from JWT session storage. Parity with React <c>AuthProvider</c> state.
/// </summary>
public sealed class CustomAuthStateProvider(JwtSessionStorage sessionStorage) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await sessionStorage.GetSessionAsync();
        return new AuthenticationState(CreatePrincipal(session));
    }

    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    internal static ClaimsPrincipal CreatePrincipal(AuthSession? session)
    {
        if (session is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>();

        if (!string.IsNullOrEmpty(session.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, session.Email));
        }

        claims.Add(new Claim(JwtParser.TenantClaimKey, session.TenantId));

        foreach (var role in session.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }
}
