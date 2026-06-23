using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Registers authenticated Security Guards page services with stubbed HTTP responses.</summary>
internal static class SecurityGuardsPageTestHelper
{
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

    internal const string SectorPickJson =
        """
        [{
          "id": "11111111-1111-1111-1111-111111111111",
          "name": "Sector A",
          "description": null,
          "requiredGuardsPerDay": 1,
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z"
        }]
        """;

    internal const string MariaAndAnaJson =
        """
        [{
          "id": "22222222-2222-2222-2222-222222222222",
          "name": "Maria Souza",
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z",
          "sectors": [{
            "id": "11111111-1111-1111-1111-111111111111",
            "name": "Sector A",
            "description": null,
            "requiredGuardsPerDay": 1,
            "isActive": true,
            "createdAt": "2026-03-02T14:30:00.000Z"
          }]
        }, {
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
        string initialUri = "/app/security-guards",
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
        services.AddSingleton(new SectorsApiClient(apiClient));
    }

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    public static HttpResponseMessage EmptyGuardsResponse() =>
        JsonResponse("[]");

    public static HttpResponseMessage DefaultGuardsResponse() =>
        JsonResponse(AnaGuardJson);

    public static HttpResponseMessage DefaultSectorsResponse() =>
        JsonResponse(SectorPickJson);

    public static HttpResponseMessage CreateGuardSuccessResponse(Guid id) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                $$"""{"id":"{{id}}"}""",
                Encoding.UTF8,
                "application/json"),
        };

    public static HttpResponseMessage NoContentResponse() =>
        new(HttpStatusCode.NoContent);

    public static HttpResponseMessage ForbiddenResponse(string message) =>
        JsonResponse($$"""{"message":"{{message}}"}""", HttpStatusCode.Forbidden);

    public static HttpResponseMessage NotFoundResponse() =>
        new(HttpStatusCode.NotFound);

    public static bool IsGuardsListGet(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.Equals("/api/security-guards", StringComparison.Ordinal);

    public static bool IsSectorsListGet(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.Equals("/api/sectors", StringComparison.Ordinal);

    public static bool IsGuardsCreatePost(HttpRequestMessage request) =>
        request.Method == HttpMethod.Post &&
        request.RequestUri!.AbsolutePath.Equals("/api/security-guards", StringComparison.Ordinal);

    public static bool IsGuardsSetSectorsPut(HttpRequestMessage request) =>
        request.Method == HttpMethod.Put &&
        request.RequestUri!.AbsolutePath.EndsWith("/sectors", StringComparison.Ordinal);

    public static HttpResponseMessage DefaultHandler(HttpRequestMessage request)
    {
        if (IsGuardsListGet(request))
        {
            return DefaultGuardsResponse();
        }

        if (IsSectorsListGet(request))
        {
            return DefaultSectorsResponse();
        }

        return NotFoundResponse();
    }
}
