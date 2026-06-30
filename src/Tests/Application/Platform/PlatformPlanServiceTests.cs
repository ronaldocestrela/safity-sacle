using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;
using SafetyScale.Infrastructure.Tenancy;

namespace SafetyScale.Tests.Application.Platform;

public class PlatformPlanServiceTests
{
    [Fact]
    public async Task CreateAsync_WithInvalidCode_ReturnsValidationFailed()
    {
        await using var db = CreateDbContext();
        var service = new PlatformPlanService(db);

        var result = await service.CreateAsync(new CreatePlatformPlanInput(
            "Starter",
            "INVALID CODE",
            null,
            99.90m,
            10,
            5));

        result.Status.Should().Be(CreatePlatformPlanStatus.ValidationFailed);
        result.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsCodeAlreadyExists()
    {
        await using var db = CreateDbContext();
        db.PlatformPlans.Add(new PlatformPlan
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Code = "starter",
            PriceMonthly = 50m,
            MaxSecurityGuards = 10,
            MaxSectors = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = new PlatformPlanService(db);
        var result = await service.CreateAsync(new CreatePlatformPlanInput(
            "Starter",
            "starter",
            null,
            99.90m,
            10,
            5));

        result.Status.Should().Be(CreatePlatformPlanStatus.CodeAlreadyExists);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidLimits_ReturnsValidationFailed()
    {
        await using var db = CreateDbContext();
        var service = new PlatformPlanService(db);

        var result = await service.CreateAsync(new CreatePlatformPlanInput(
            "Starter",
            "starter",
            null,
            99.90m,
            0,
            0));

        result.Status.Should().Be(CreatePlatformPlanStatus.ValidationFailed);
        result.Errors.Should().Contain(e => e.Contains("seguranças"));
        result.Errors.Should().Contain(e => e.Contains("setores"));
    }

    [Fact]
    public async Task SetActiveAsync_WhenPlanExists_UpdatesStatus()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        db.PlatformPlans.Add(new PlatformPlan
        {
            Id = planId,
            Name = "Business",
            Code = "business",
            PriceMonthly = 199m,
            MaxSecurityGuards = 20,
            MaxSectors = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = new PlatformPlanService(db);
        var result = await service.SetActiveAsync(planId, isActive: false);

        result.Status.Should().Be(SetPlatformPlanActiveStatus.Success);
        var plan = await db.PlatformPlans.SingleAsync(p => p.Id == planId);
        plan.IsActive.Should().BeFalse();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"platform-plans-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options, new BypassTenantExecutionContext());
    }
}
