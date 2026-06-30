using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Tenancy;

public sealed class PlanLimitEvaluator(
    ApplicationDbContext dbContext,
    ITenantExecutionContext tenantExecution,
    ISecurityGuardRepository securityGuardRepository,
    ISectorRepository sectorRepository) : IPlanLimitEvaluator
{
    public async Task<PlanLimitEvaluation> EvaluateCreateSecurityGuardAsync(
        CancellationToken cancellationToken = default)
    {
        var limits = await GetCurrentTenantPlanLimitsAsync(cancellationToken);
        if (limits is null)
        {
            return PlanLimitEvaluation.Allowed();
        }

        var currentCount = await securityGuardRepository.CountAsync(cancellationToken);
        if (currentCount >= limits.Value.MaxSecurityGuards)
        {
            return PlanLimitEvaluation.Denied(
                $"Limite de seguranças do plano atingido ({limits.Value.MaxSecurityGuards}).");
        }

        return PlanLimitEvaluation.Allowed();
    }

    public async Task<PlanLimitEvaluation> EvaluateCreateSectorAsync(
        CancellationToken cancellationToken = default)
    {
        var limits = await GetCurrentTenantPlanLimitsAsync(cancellationToken);
        if (limits is null)
        {
            return PlanLimitEvaluation.Allowed();
        }

        var currentCount = await sectorRepository.CountAsync(cancellationToken);
        if (currentCount >= limits.Value.MaxSectors)
        {
            return PlanLimitEvaluation.Denied(
                $"Limite de setores do plano atingido ({limits.Value.MaxSectors}).");
        }

        return PlanLimitEvaluation.Allowed();
    }

    public async Task<PlanLimitEvaluation> EvaluatePlanAssignmentAsync(
        Guid tenantId,
        Guid? newPlanId,
        CancellationToken cancellationToken = default)
    {
        if (newPlanId is null)
        {
            return PlanLimitEvaluation.Allowed();
        }

        var plan = await dbContext.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == newPlanId.Value, cancellationToken);

        if (plan is null)
        {
            return PlanLimitEvaluation.Allowed();
        }

        var securityGuardCount = await dbContext.SecurityGuards
            .IgnoreQueryFilters()
            .CountAsync(g => g.TenantId == tenantId, cancellationToken);

        var sectorCount = await dbContext.Sectors
            .IgnoreQueryFilters()
            .CountAsync(s => s.TenantId == tenantId, cancellationToken);

        var errors = new List<string>();

        if (securityGuardCount > plan.MaxSecurityGuards)
        {
            errors.Add(
                $"O tenant possui {securityGuardCount} seguranças, mas o plano permite no máximo {plan.MaxSecurityGuards}.");
        }

        if (sectorCount > plan.MaxSectors)
        {
            errors.Add(
                $"O tenant possui {sectorCount} setores, mas o plano permite no máximo {plan.MaxSectors}.");
        }

        if (errors.Count > 0)
        {
            return PlanLimitEvaluation.Denied(string.Join(' ', errors));
        }

        return PlanLimitEvaluation.Allowed();
    }

    private async Task<(int MaxSecurityGuards, int MaxSectors)?> GetCurrentTenantPlanLimitsAsync(
        CancellationToken cancellationToken)
    {
        if (!tenantExecution.IsTenantIsolationEnabled || tenantExecution.TenantId is null)
        {
            return null;
        }

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Include(t => t.PlatformPlan)
            .FirstOrDefaultAsync(t => t.Id == tenantExecution.TenantId.Value, cancellationToken);

        if (tenant?.PlatformPlan is null)
        {
            return null;
        }

        return (tenant.PlatformPlan.MaxSecurityGuards, tenant.PlatformPlan.MaxSectors);
    }
}
