using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Registers authenticated dashboard page services with stubbed HTTP responses.</summary>
internal static class AppDashboardTestHelper
{
    public static void Register(
        IServiceCollection services,
        string initialUri = "/app",
        params UserRole[] roles)
    {
        var js = new FakeJsRuntime();
        var jsonOptions = AppJsonSerializerOptions.Create();
        var browserStorage = new BrowserSessionStorage(js, jsonOptions);
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var authProvider = new CustomAuthStateProvider(sessionStorage);
        var navigation = new TestNavigationManager(initialUri);

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

        services.AddSingleton(navigation);
        services.AddSingleton<NavigationManager>(navigation);
        services.AddSingleton(browserStorage);
        services.AddSingleton(sessionStorage);
        services.AddSingleton(authProvider);
        services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var urlBuilder = services.BuildServiceProvider().GetRequiredService<ApiUrlBuilder>();
        var httpClient = new HttpClient(new FuncHttpMessageHandler(DashboardResponseFactory))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        var apiClient = new ApiHttpClient(httpClient, urlBuilder, jsonOptions);

        services.AddSingleton(apiClient);
        services.AddSingleton(new AuthSessionService(apiClient, sessionStorage, authProvider, navigation));
        services.AddSingleton(new SecurityGuardsApiClient(apiClient));
        services.AddSingleton(new SchedulesApiClient(apiClient));
    }

    private static HttpResponseMessage DashboardResponseFactory(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/api/security-guards", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            };
        }

        if (path.Contains("/api/schedules/month/", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
