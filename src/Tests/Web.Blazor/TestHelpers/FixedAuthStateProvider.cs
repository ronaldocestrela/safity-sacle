using Microsoft.AspNetCore.Components.Authorization;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>AuthenticationStateProvider with a fixed state for bUnit component tests.</summary>
internal sealed class FixedAuthStateProvider(AuthenticationState state) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
}
