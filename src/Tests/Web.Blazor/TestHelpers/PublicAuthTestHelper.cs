using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>HTTP handler that returns a response from a delegate (route-aware stubs for auth page tests).</summary>
internal sealed class FuncHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(handler(request));
}

/// <summary>Registers public auth page services with stubbed HTTP responses.</summary>
internal static class PublicAuthTestHelper
{
    internal sealed record PublicAuthServices(
        TestNavigationManager Navigation,
        AuthSessionService AuthService,
        TenantsRegistrationClient RegistrationClient);

    public static PublicAuthServices Register(
        IServiceCollection services,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string initialUri = "/login")
    {
        var js = new FakeJsRuntime();
        var jsonOptions = AppJsonSerializerOptions.Create();
        var browserStorage = new BrowserSessionStorage(js, jsonOptions);
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var authProvider = new CustomAuthStateProvider(sessionStorage);
        var navigation = new TestNavigationManager(initialUri);

        services.AddSingleton(navigation);
        services.AddSingleton<NavigationManager>(navigation);
        services.AddSingleton(browserStorage);
        services.AddSingleton(sessionStorage);
        services.AddSingleton(authProvider);
        services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var urlBuilder = services.BuildServiceProvider().GetRequiredService<ApiUrlBuilder>();
        var httpClient = new HttpClient(new FuncHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        var apiClient = new ApiHttpClient(httpClient, urlBuilder, jsonOptions);

        var authService = new AuthSessionService(apiClient, sessionStorage, authProvider, navigation);
        var registrationClient = new TenantsRegistrationClient(apiClient);

        services.AddSingleton(authService);
        services.AddSingleton(registrationClient);

        return new PublicAuthServices(navigation, authService, registrationClient);
    }

    public static HttpResponseMessage LoginSuccessResponse(string email = "user@test.com")
    {
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = JwtTestUtils.ExpSoon(),
            ["email"] = email,
            ["role"] = "Admin",
        });

        var json = JsonSerializer.Serialize(new { token });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    public static HttpResponseMessage LoginUnauthorizedResponse() =>
        new(HttpStatusCode.Unauthorized);

    public static HttpResponseMessage SignupEmailConflictResponse() =>
        new(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"message":"Este e-mail já está cadastrado."}""",
                Encoding.UTF8,
                "application/json"),
        };

    public static HttpResponseMessage NotFoundResponse() =>
        new(HttpStatusCode.NotFound);
}
