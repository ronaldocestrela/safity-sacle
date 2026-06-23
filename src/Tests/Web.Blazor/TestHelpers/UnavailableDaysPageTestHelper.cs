using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Registers authenticated Unavailable Days page services with stubbed HTTP responses.</summary>
internal static class UnavailableDaysPageTestHelper
{
    internal static readonly Guid DefaultGuardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    internal const string AnaGuardJson =
        """
        [{
          "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          "name": "Ana Costa",
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z",
          "sectors": []
        }]
        """;

    public static void Register(
        IServiceCollection services,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string initialUri = "/app/unavailable-days",
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
        var httpClient = new HttpClient(new FuncHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        var apiClient = new ApiHttpClient(httpClient, urlBuilder, jsonOptions);

        services.AddSingleton(apiClient);
        services.AddSingleton(new AuthSessionService(apiClient, sessionStorage, authProvider, navigation));
        services.AddSingleton(new SecurityGuardsApiClient(apiClient));
        services.AddSingleton(new UnavailableDaysApiClient(apiClient));
    }

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    public static HttpResponseMessage EmptyDaysResponse() => JsonResponse("[]");

    public static HttpResponseMessage DefaultGuardsResponse() => JsonResponse(AnaGuardJson);

    public static HttpResponseMessage ForbiddenResponse(string message) =>
        JsonResponse($$"""{"message":"{{message}}"}""", HttpStatusCode.Forbidden);

    public static HttpResponseMessage ConflictResponse(string message) =>
        JsonResponse($$"""{"message":"{{message}}"}""", HttpStatusCode.Conflict);

    public static HttpResponseMessage CreatedDayResponse(Guid id) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                $$"""{"id":"{{id}}"}""",
                Encoding.UTF8,
                "application/json"),
        };

    public static HttpResponseMessage NoContentResponse() => new(HttpStatusCode.NoContent);

    public static HttpResponseMessage NotFoundResponse() => new(HttpStatusCode.NotFound);

    public static bool IsGuardsListGet(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.Equals("/api/security-guards", StringComparison.Ordinal);

    public static bool IsDaysListGet(HttpRequestMessage request, Guid guardId) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.Equals(
            $"/api/security-guards/{guardId}/unavailable-days",
            StringComparison.Ordinal);

    public static bool IsDayAddPost(HttpRequestMessage request, Guid guardId) =>
        request.Method == HttpMethod.Post &&
        request.RequestUri!.AbsolutePath.Equals(
            $"/api/security-guards/{guardId}/unavailable-days",
            StringComparison.Ordinal);

    public static bool IsDayDelete(HttpRequestMessage request) =>
        request.Method == HttpMethod.Delete &&
        request.RequestUri!.AbsolutePath.StartsWith("/api/unavailable-days/", StringComparison.Ordinal);

    public static string KeyDay1()
    {
        var d = DateTime.Now;
        return $"{d.Year:D4}-{d.Month:D2}-01";
    }

    public static string KeyDay5()
    {
        var d = DateTime.Now;
        return $"{d.Year:D4}-{d.Month:D2}-05";
    }

    public static HttpResponseMessage DefaultHandler(HttpRequestMessage request)
    {
        if (IsGuardsListGet(request))
        {
            return DefaultGuardsResponse();
        }

        if (IsDaysListGet(request, DefaultGuardId))
        {
            return EmptyDaysResponse();
        }

        return NotFoundResponse();
    }
}
