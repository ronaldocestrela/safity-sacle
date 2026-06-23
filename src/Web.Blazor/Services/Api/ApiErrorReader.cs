using System.Text.Json;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Best-effort message extraction from ASP.NET ProblemDetails / FluentValidation payloads.
/// Parity with React <c>readApiErrorMessage</c>.
/// </summary>
public static class ApiErrorReader
{
    public static async Task<string?> ReadMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseJsonBody(body);
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.StartsWith('{'))
        {
            return ParseJsonBody(trimmed) ?? trimmed;
        }

        return trimmed;
    }

    internal static string? ParseJsonBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return ExtractMessage(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractMessage(JsonElement root)
    {
        if (root.TryGetProperty("errors", out var errorsElement))
        {
            var fromErrors = ReadErrors(errorsElement);
            if (!string.IsNullOrWhiteSpace(fromErrors))
            {
                return fromErrors;
            }
        }

        foreach (var key in new[] { "detail", "title", "message" })
        {
            if (root.TryGetProperty(key, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString()?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static string? ReadErrors(JsonElement errorsElement)
    {
        if (errorsElement.ValueKind == JsonValueKind.Array)
        {
            var messages = errorsElement.EnumerateArray()
                .Select(e => e.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return messages.Count > 0 ? string.Join(' ', messages) : null;
        }

        if (errorsElement.ValueKind == JsonValueKind.Object)
        {
            var messages = new List<string>();
            foreach (var property in errorsElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    messages.AddRange(
                        property.Value.EnumerateArray()
                            .Select(e => e.ToString())
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var text = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        messages.Add(text);
                    }
                }
            }

            return messages.Count > 0 ? string.Join(' ', messages) : null;
        }

        return null;
    }
}
