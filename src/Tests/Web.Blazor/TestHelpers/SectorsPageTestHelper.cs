using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Registers authenticated Sectors page services with stubbed HTTP responses.</summary>
internal static class SectorsPageTestHelper
{
    internal const string PerimeterSectorJson =
        """
        [{
          "id": "11111111-1111-1111-1111-111111111111",
          "name": "Perimeter",
          "description": "Outer ring",
          "requiredGuardsPerDay": 2,
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z"
        }]
        """;

    internal const string LobbyAndPerimeterJson =
        """
        [{
          "id": "22222222-2222-2222-2222-222222222222",
          "name": "Lobby",
          "description": null,
          "requiredGuardsPerDay": 3,
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z"
        }, {
          "id": "11111111-1111-1111-1111-111111111111",
          "name": "Perimeter",
          "description": "Outer ring",
          "requiredGuardsPerDay": 2,
          "isActive": true,
          "createdAt": "2026-03-02T14:30:00.000Z"
        }]
        """;

    public static void Register(
        IServiceCollection services,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string initialUri = "/app/sectors",
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
        var httpClient = new HttpClient(new FuncHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        var apiClient = new ApiHttpClient(httpClient, urlBuilder, jsonOptions);

        services.AddSingleton(apiClient);
        services.AddSingleton(new AuthSessionService(apiClient, sessionStorage, authProvider, navigation));
        services.AddSingleton(new SectorsApiClient(apiClient));
    }

    public static HttpResponseMessage SectorListResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    public static HttpResponseMessage EmptyListResponse() =>
        SectorListResponse("[]");

    public static HttpResponseMessage DefaultListResponse() =>
        SectorListResponse(PerimeterSectorJson);

    public static HttpResponseMessage CreateSectorSuccessResponse(Guid id) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                $$"""{"id":"{{id}}"}""",
                Encoding.UTF8,
                "application/json"),
        };

    public static HttpResponseMessage NotFoundResponse() =>
        new(HttpStatusCode.NotFound);

    public static bool IsSectorsListGet(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.Equals("/api/sectors", StringComparison.Ordinal);

    public static bool IsSectorsCreatePost(HttpRequestMessage request) =>
        request.Method == HttpMethod.Post &&
        request.RequestUri!.AbsolutePath.Equals("/api/sectors", StringComparison.Ordinal);
}
