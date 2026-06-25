using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Web.Blazor.Services.Auth;

public sealed class CustomAuthStateProvider(
    JwtSessionStorage tenantSessionStorage,
    PlatformJwtSessionStorage platformSessionStorage,
    NavigationManager navigationManager) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (IsPlatformRoute())
        {
            var platformSession = await platformSessionStorage.GetSessionAsync();
            return new AuthenticationState(CreatePlatformPrincipal(platformSession));
        }

        var session = await tenantSessionStorage.GetSessionAsync();
        return new AuthenticationState(CreateTenantPrincipal(session));
    }

    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    internal bool IsPlatformRoute()
    {
        var relative = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        return relative.StartsWith("platform/", StringComparison.OrdinalIgnoreCase) ||
               relative.Equals("platform", StringComparison.OrdinalIgnoreCase);
    }

    internal static ClaimsPrincipal CreateTenantPrincipal(AuthSession? session)
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
        claims.Add(new Claim(JwtParser.UserKindClaimKey, "Tenant"));

        foreach (var role in session.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
    }

    internal static ClaimsPrincipal CreatePlatformPrincipal(PlatformAuthSession? session)
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

        claims.Add(new Claim(JwtParser.UserKindClaimKey, "Platform"));

        foreach (var role in session.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
    }
}
