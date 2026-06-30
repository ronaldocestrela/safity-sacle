using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;
using SafetyScale.Infrastructure.Persistence.Repositories;
using SafetyScale.Infrastructure.Tenancy;

namespace SafetyScale.Tests.Application.Platform;

public class PlanLimitEvaluatorTests
{
    [Fact]
    public async Task EvaluateCreateSecurityGuardAsync_WhenAtLimit_ReturnsDenied()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        SeedTenantWithPlan(db, tenantId, planId, maxSecurityGuards: 1, maxSectors: 5);
        db.SecurityGuards.Add(new SecurityGuard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Existing Guard",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, tenantId);
        var result = await evaluator.EvaluateCreateSecurityGuardAsync();

        result.IsAllowed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Limite de seguranças");
    }

    [Fact]
    public async Task EvaluateCreateSectorAsync_WhenAtLimit_ReturnsDenied()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        SeedTenantWithPlan(db, tenantId, planId, maxSecurityGuards: 10, maxSectors: 1);
        db.Sectors.Add(new Sector
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Primary",
            RequiredGuardsPerDay = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, tenantId);
        var result = await evaluator.EvaluateCreateSectorAsync();

        result.IsAllowed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Limite de setores");
    }

    [Fact]
    public async Task EvaluatePlanAssignmentAsync_WhenUsageExceedsTargetPlan_ReturnsDenied()
    {
        var tenantId = Guid.NewGuid();
        var currentPlanId = Guid.NewGuid();
        var smallerPlanId = Guid.NewGuid();
        await using var db = CreateDbContext(tenantId);
        SeedTenantWithPlan(db, tenantId, currentPlanId, maxSecurityGuards: 10, maxSectors: 10);
        db.PlatformPlans.Add(new PlatformPlan
        {
            Id = smallerPlanId,
            Name = "Small",
            Code = "small",
            PriceMonthly = 49m,
            MaxSecurityGuards = 1,
            MaxSectors = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        db.SecurityGuards.AddRange(
            new SecurityGuard
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Guard 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new SecurityGuard
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Guard 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        db.Sectors.AddRange(
            new Sector
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Sector 1",
                RequiredGuardsPerDay = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Sector
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Sector 2",
                RequiredGuardsPerDay = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, tenantId);
        var result = await evaluator.EvaluatePlanAssignmentAsync(tenantId, smallerPlanId);

        result.IsAllowed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("seguranças");
        result.ErrorMessage.Should().Contain("setores");
    }

    private static PlanLimitEvaluator CreateEvaluator(ApplicationDbContext db, Guid tenantId)
    {
        var tenantContext = new FixedTenantExecutionContext(tenantId);
        return new PlanLimitEvaluator(
            db,
            tenantContext,
            new SecurityGuardRepository(db),
            new SectorRepository(db));
    }

    private static void SeedTenantWithPlan(
        ApplicationDbContext db,
        Guid tenantId,
        Guid planId,
        int maxSecurityGuards,
        int maxSectors)
    {
        db.PlatformPlans.Add(new PlatformPlan
        {
            Id = planId,
            Name = "Business",
            Code = $"plan-{planId:N}"[..20],
            PriceMonthly = 199m,
            MaxSecurityGuards = maxSecurityGuards,
            MaxSectors = maxSectors,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Tenant",
            Slug = $"tenant-{tenantId:N}"[..20],
            PlatformPlanId = planId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static ApplicationDbContext CreateDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"plan-limits-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options, new FixedTenantExecutionContext(tenantId));
    }

    private sealed class FixedTenantExecutionContext(Guid tenantId) : ITenantExecutionContext
    {
        public bool IsTenantIsolationEnabled => true;

        public Guid? TenantId => tenantId;

        public void SetExecutingTenant(Guid tenantId) { }

        public void ClearTenant() { }
    }
}
