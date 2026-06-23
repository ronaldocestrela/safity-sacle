using System.Security.Claims;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Shared bUnit TestContext setup for Blazor component tests.</summary>
public abstract class BlazorComponentTestBase : TestContext
{
    protected BlazorComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddAuthorizationCore();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = string.Empty })
            .Build();
        Services.AddSingleton<IConfiguration>(configuration);
        Services.AddSingleton<AppConfiguration>();
        Services.AddSingleton<ApiUrlBuilder>();
        Services.AddSingleton(AppJsonSerializerOptions.Create());
    }

    protected TestNavigationManager RegisterNavigation(string relativePath)
    {
        var nav = new TestNavigationManager(relativePath);
        Services.AddSingleton<NavigationManager>(nav);
        return nav;
    }

    protected void RegisterAnonymousAuth()
    {
        var state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        Services.AddSingleton<AuthenticationStateProvider>(new FixedAuthStateProvider(state));
    }

    protected void RegisterAuthenticatedAuth(params UserRole[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        var state = new AuthenticationState(new ClaimsPrincipal(identity));
        Services.AddSingleton<AuthenticationStateProvider>(new FixedAuthStateProvider(state));
    }

    protected void RegisterAuthSessionServices(string initialUri, params UserRole[] roles)
    {
        var js = new FakeJsRuntime();
        var browserStorage = new BrowserSessionStorage(js, AppJsonSerializerOptions.Create());
        var sessionStorage = new JwtSessionStorage(browserStorage);

        object roleClaim = roles.Length switch
        {
            0 => Array.Empty<string>(),
            1 => roles[0].ToString()!,
            _ => roles.Select(r => r.ToString()).ToArray(),
        };

        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = JwtTestUtils.ExpSoon(),
            ["email"] = "user@example.com",
            ["role"] = roleClaim,
        });

        sessionStorage.SaveTokenAsync(token).GetAwaiter().GetResult();

        var nav = RegisterNavigation(initialUri);

        Services.AddSingleton(browserStorage);
        Services.AddSingleton(sessionStorage);
        Services.AddSingleton<CustomAuthStateProvider>();
        Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

        Services.AddSingleton<AuthSessionService>(sp =>
        {
            var apiClient = new ApiHttpClient(
                new HttpClient { BaseAddress = new Uri("http://localhost/") },
                sp.GetRequiredService<ApiUrlBuilder>(),
                sp.GetRequiredService<System.Text.Json.JsonSerializerOptions>());

            return new AuthSessionService(
                apiClient,
                sp.GetRequiredService<JwtSessionStorage>(),
                sp.GetRequiredService<CustomAuthStateProvider>(),
                nav);
        });
    }

    protected static bool HasActiveClass(IElement link) =>
        link.ClassList.Any(c => c.Contains("bottom-link-active", StringComparison.Ordinal));
}
