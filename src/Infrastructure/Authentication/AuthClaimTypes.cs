namespace SafetyScale.Infrastructure.Authentication;

public static class AuthClaimTypes
{
    public const string UserKind = "user_kind";

    /// <summary>JWT claim carrying the linked security guard identifier for scoped operators.</summary>
    public const string SecurityGuardId = "security_guard_id";
}
