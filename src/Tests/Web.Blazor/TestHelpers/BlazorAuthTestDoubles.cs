using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

internal sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    public string? LastUri { get; private set; }

    public bool LastReplace { get; private set; }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        LastUri = uri;
        LastReplace = false;
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        LastUri = uri;
        LastReplace = options.ReplaceHistoryEntry;
    }
}

internal static class AuthNotificationTracker
{
    public static (CustomAuthStateProvider Provider, Func<int> Count) Create(JwtSessionStorage sessionStorage)
    {
        var provider = new CustomAuthStateProvider(sessionStorage);
        var count = 0;
        provider.AuthenticationStateChanged += _ => count++;

        return (provider, () => count);
    }
}
