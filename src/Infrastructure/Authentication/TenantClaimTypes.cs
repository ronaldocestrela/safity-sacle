namespace SafetyScale.Infrastructure.Authentication;

public static class TenantClaimTypes
{
    /// <summary>JWT claim emitted on login carrying the authenticated tenant identifier.</summary>
    public const string TenantId = "tenant_id";
}
