namespace SafetyScale.Infrastructure.Messaging.Email;

public static class EmailRetryPolicy
{
    public static DateTime CalculateNextAvailableUtc(
        int attempts,
        int initialDelaySeconds,
        int maxDelaySeconds,
        DateTime utcNow)
    {
        if (attempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts), "Attempts must be at least 1.");
        }

        if (initialDelaySeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelaySeconds));
        }

        if (maxDelaySeconds < initialDelaySeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelaySeconds));
        }

        var multiplier = Math.Pow(2, attempts - 1);
        var delaySeconds = (int)Math.Min(initialDelaySeconds * multiplier, maxDelaySeconds);
        return utcNow.AddSeconds(delaySeconds);
    }
}
