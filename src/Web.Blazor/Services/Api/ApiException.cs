namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Non-success API response with a parsed message when available.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string? apiMessage, string? fallbackMessage = null)
        : base(apiMessage ?? fallbackMessage ?? $"HTTP {statusCode}")
    {
        StatusCode = statusCode;
        ApiMessage = apiMessage;
    }

    public int StatusCode { get; }

    public string? ApiMessage { get; }
}
