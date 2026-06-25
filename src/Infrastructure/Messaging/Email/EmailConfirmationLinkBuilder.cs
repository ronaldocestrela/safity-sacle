namespace SafetyScale.Infrastructure.Messaging.Email;

public static class EmailConfirmationLinkBuilder
{
    public static string Build(string webBaseUrl, string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(webBaseUrl))
        {
            throw new InvalidOperationException("Public web base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        var baseUrl = webBaseUrl.TrimEnd('/');
        return
            $"{baseUrl}/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }
}
