using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Infrastructure.Persistence;

/// <summary>For design-time tooling and EF migrations; disables tenant isolation.
/// </summary>
public sealed class BypassTenantExecutionContext : ITenantExecutionContext
{
    public bool IsTenantIsolationEnabled => false;

    public Guid? TenantId => null;

    public void SetExecutingTenant(Guid tenantId)
    {
    }

    public void ClearTenant()
    {
    }
}
