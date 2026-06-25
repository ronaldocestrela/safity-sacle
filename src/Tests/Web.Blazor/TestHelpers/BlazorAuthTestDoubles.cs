using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
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

internal static class BlazorAuthTestFactory
{
    internal sealed record AuthStack(
        BrowserSessionStorage BrowserStorage,
        PlatformBrowserSessionStorage PlatformBrowserStorage,
        JwtSessionStorage TenantSessionStorage,
        PlatformJwtSessionStorage PlatformSessionStorage,
        CustomAuthStateProvider AuthProvider,
        TestNavigationManager Navigation);

    internal static AuthStack CreateAuthStack(string? initialUri = null)
    {
        var js = new FakeJsRuntime();
        var jsonOptions = AppJsonSerializerOptions.Create();
        var browserStorage = new BrowserSessionStorage(js, jsonOptions);
        var platformBrowserStorage = new PlatformBrowserSessionStorage(js, jsonOptions);
        var tenantSessionStorage = new JwtSessionStorage(browserStorage);
        var platformSessionStorage = new PlatformJwtSessionStorage(platformBrowserStorage);
        var navigation = new TestNavigationManager(initialUri);
        var authProvider = new CustomAuthStateProvider(
            tenantSessionStorage,
            platformSessionStorage,
            navigation);

        return new AuthStack(
            browserStorage,
            platformBrowserStorage,
            tenantSessionStorage,
            platformSessionStorage,
            authProvider,
            navigation);
    }

    internal static void RegisterAuthServices(IServiceCollection services, AuthStack authStack)
    {
        services.AddSingleton(authStack.Navigation);
        services.AddSingleton<NavigationManager>(authStack.Navigation);
        services.AddSingleton(authStack.BrowserStorage);
        services.AddSingleton(authStack.PlatformBrowserStorage);
        services.AddSingleton(authStack.TenantSessionStorage);
        services.AddSingleton(authStack.PlatformSessionStorage);
        services.AddSingleton(authStack.AuthProvider);
        services.AddSingleton<AuthenticationStateProvider>(authStack.AuthProvider);
    }

    internal static string CreateTenantToken(params UserRole[] roles)
    {
        object roleClaim = roles.Length switch
        {
            0 => Array.Empty<string>(),
            1 => roles[0].ToString()!,
            _ => roles.Select(r => r.ToString()).ToArray(),
        };

        return JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = JwtTestUtils.ExpSoon(),
            ["email"] = "user@example.com",
            ["role"] = roleClaim,
            ["tenant_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ["user_kind"] = "Tenant",
        });
    }
}

internal static class AuthNotificationTracker
{
    public static (CustomAuthStateProvider Provider, Func<int> Count) Create(JwtSessionStorage sessionStorage)
    {
        var stack = BlazorAuthTestFactory.CreateAuthStack();
        var provider = stack.AuthProvider;
        var count = 0;
        provider.AuthenticationStateChanged += _ => count++;

        return (provider, () => count);
    }
}
