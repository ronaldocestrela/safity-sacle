using System.Text.Json;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Global JSON options for API payloads. Parity with API <c>AddJsonOptions</c> (camelCase, case-insensitive).
/// </summary>
public static class AppJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
    }
}
