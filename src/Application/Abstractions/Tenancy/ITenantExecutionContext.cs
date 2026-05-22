namespace SafetyScale.Application.Abstractions.Tenancy;

/// <summary>
/// Tracks whether EF Core tenant filters should isolate rows per request lifecycle.
/// </summary>
public interface ITenantExecutionContext
{
    /// <summary>When true, global query filters constrain rows for the current tenant.</summary>
    bool IsTenantIsolationEnabled { get; }

    /// <summary>Resolved tenant identifier for the authenticated request.</summary>
    Guid? TenantId { get; }

    void SetExecutingTenant(Guid tenantId);

    void ClearTenant();
}
