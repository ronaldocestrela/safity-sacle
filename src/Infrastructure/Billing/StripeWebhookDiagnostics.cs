using System.Text.Json;

namespace SafetyScale.Infrastructure.Billing;

public static class StripeWebhookDiagnostics
{
    public static string? TryExtractApiVersion(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonPayload);
            if (document.RootElement.TryGetProperty("api_version", out var apiVersion) &&
                apiVersion.ValueKind == JsonValueKind.String)
            {
                return apiVersion.GetString();
            }
        }
        catch (JsonException)
        {
            // ignore malformed payloads; signature validation will fail separately
        }

        return null;
    }

    public static string? TryExtractEventId(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonPayload);
            if (document.RootElement.TryGetProperty("id", out var eventId) &&
                eventId.ValueKind == JsonValueKind.String)
            {
                return eventId.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public static string? TryExtractEventType(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonPayload);
            if (document.RootElement.TryGetProperty("type", out var eventType) &&
                eventType.ValueKind == JsonValueKind.String)
            {
                return eventType.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
