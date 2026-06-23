namespace SafetyScale.Web.Blazor.Services;

/// <summary>User-facing display helpers. Parity with React <c>userDisplay.ts</c>.</summary>
public static class UserDisplay
{
    public static string UserInitials(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "?";
        }

        var local = email.Split('@')[0];
        var parts = local
            .Split(['.', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 0)
            .ToArray();

        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()[..2];
        }

        return email.Length >= 2
            ? email[..2].ToUpperInvariant()
            : email.ToUpperInvariant();
    }
}
