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
        var authStack = BlazorAuthTestFactory.CreateAuthStack(initialUri);
        var sessionStorage = authStack.TenantSessionStorage;
        var authProvider = authStack.AuthProvider;
        var navigation = authStack.Navigation;
        var jsonOptions = AppJsonSerializerOptions.Create();

        sessionStorage.SaveTokenAsync(BlazorAuthTestFactory.CreateTenantToken(roles))
            .GetAwaiter()
            .GetResult();

        BlazorAuthTestFactory.RegisterAuthServices(services, authStack);

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
