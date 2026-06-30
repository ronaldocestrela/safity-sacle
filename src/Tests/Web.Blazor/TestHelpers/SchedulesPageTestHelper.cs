using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

/// <summary>Registers authenticated Schedules page services with stubbed HTTP responses.</summary>
internal static class SchedulesPageTestHelper
{
    internal static readonly Guid SampleScheduleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    internal const string SampleScheduleJson =
        """
        {
          "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          "month": 5,
          "year": 2026,
          "generatedAt": "2026-05-01T10:00:00.000Z",
          "items": [{
            "id": "11111111-1111-1111-1111-111111111111",
            "securityGuardId": "22222222-2222-2222-2222-222222222222",
            "securityGuardName": "Pat Smith",
            "securityGuardIsActive": true,
            "sectorId": "33333333-3333-3333-3333-333333333333",
            "sectorName": "Primary",
            "date": "2026-05-07",
            "isWeekend": false
          }, {
            "id": "44444444-4444-4444-4444-444444444444",
            "securityGuardId": "55555555-5555-5555-5555-555555555555",
            "securityGuardName": "Alex Inactive",
            "securityGuardIsActive": false,
            "sectorId": "33333333-3333-3333-3333-333333333333",
            "sectorName": "Primary",
            "date": "2026-05-10",
            "isWeekend": true
          }]
        }
        """;

    internal const string EmptyItemsScheduleJson =
        """
        {
          "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          "month": 5,
          "year": 2026,
          "generatedAt": "2026-05-01T10:00:00.000Z",
          "items": []
        }
        """;

    public static void Register(
        IServiceCollection services,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string initialUri = "/app/schedules",
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
        services.AddSingleton(new SchedulesApiClient(apiClient));
    }

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    public static HttpResponseMessage NotFoundResponse() => new(HttpStatusCode.NotFound);

    public static HttpResponseMessage GenerateSuccessResponse(Guid id) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                $$"""{"id":"{{id}}"}""",
                Encoding.UTF8,
                "application/json"),
        };

    public static HttpResponseMessage ConflictResponse() => new(HttpStatusCode.Conflict);

    public static HttpResponseMessage CoverageFailureResponse(string message, string? failedDate = "2026-05-02") =>
        JsonResponse(
            $$"""
            {
              "code": "ScheduleCoverageFailed",
              "message": "{{message}}",
              "failedDate": "{{failedDate}}"
            }
            """,
            HttpStatusCode.BadRequest);

    public static bool IsScheduleByMonthYearGet(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get &&
        request.RequestUri!.AbsolutePath.StartsWith("/api/schedules/month/", StringComparison.Ordinal);

    public static bool IsScheduleGeneratePost(HttpRequestMessage request) =>
        request.Method == HttpMethod.Post &&
        request.RequestUri!.AbsolutePath.Equals("/api/schedules/generate", StringComparison.Ordinal);

    public static (int Month, int Year) CurrentPeriod()
    {
        var now = DateTime.Now;
        return (now.Month, now.Year);
    }

    public static HttpResponseMessage DefaultHandler(HttpRequestMessage request)
    {
        if (IsScheduleByMonthYearGet(request))
        {
            return NotFoundResponse();
        }

        return NotFoundResponse();
    }
}
