using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Infrastructure.Persistence;

public sealed class TenantExecutionContext : ITenantExecutionContext
{
    private Guid? _tenantId;

    public bool IsTenantIsolationEnabled => _tenantId.HasValue;

    public Guid? TenantId => _tenantId;

    public void SetExecutingTenant(Guid tenantId) => _tenantId = tenantId;

    public void ClearTenant() => _tenantId = null;
}
