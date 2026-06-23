using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

public sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager(string? initialUri = null)
    {
        var uri = string.IsNullOrWhiteSpace(initialUri) ? "http://localhost/" : ToInitialAbsoluteUri(initialUri);
        Initialize(uri, uri);
    }

    private static string ToInitialAbsoluteUri(string pathOrUri)
    {
        if (pathOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUri;
        }

        return pathOrUri.StartsWith('/')
            ? $"http://localhost{pathOrUri}"
            : $"http://localhost/{pathOrUri}";
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
