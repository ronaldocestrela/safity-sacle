using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Tenancy;

public sealed partial class PlatformPlanService(ApplicationDbContext dbContext) : IPlatformPlanService
{
    public async Task<IReadOnlyList<PlatformPlanSummaryDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformPlans
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => ToSummary(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformPlanSummaryDto>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => ToSummary(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreatePlatformPlanResult> CreateAsync(
        CreatePlatformPlanInput input,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCreateInput(input);
        if (validationErrors.Count > 0)
        {
            return new CreatePlatformPlanResult(
                CreatePlatformPlanStatus.ValidationFailed,
                Errors: validationErrors);
        }

        var normalizedCode = NormalizeCode(input.Code);
        var codeTaken = await dbContext.PlatformPlans
            .AnyAsync(p => p.Code == normalizedCode, cancellationToken);

        if (codeTaken)
        {
            return new CreatePlatformPlanResult(CreatePlatformPlanStatus.CodeAlreadyExists);
        }

        var plan = new PlatformPlan
        {
            Id = Guid.NewGuid(),
            Name = input.Name.Trim(),
            Code = normalizedCode,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            PriceMonthly = input.PriceMonthly,
            MaxSecurityGuards = input.MaxSecurityGuards,
            MaxSectors = input.MaxSectors,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.PlatformPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePlatformPlanResult(CreatePlatformPlanStatus.Success, plan.Id);
    }

    public async Task<UpdatePlatformPlanResult> UpdateAsync(
        Guid planId,
        UpdatePlatformPlanInput input,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateInput(input);
        if (validationErrors.Count > 0)
        {
            return new UpdatePlatformPlanResult(
                UpdatePlatformPlanStatus.ValidationFailed,
                Errors: validationErrors);
        }

        var plan = await dbContext.PlatformPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan is null)
        {
            return new UpdatePlatformPlanResult(UpdatePlatformPlanStatus.NotFound);
        }

        plan.Name = input.Name.Trim();
        plan.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        plan.PriceMonthly = input.PriceMonthly;
        plan.MaxSecurityGuards = input.MaxSecurityGuards;
        plan.MaxSectors = input.MaxSectors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new UpdatePlatformPlanResult(UpdatePlatformPlanStatus.Success);
    }

    public async Task<SetPlatformPlanActiveResult> SetActiveAsync(
        Guid planId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.PlatformPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan is null)
        {
            return new SetPlatformPlanActiveResult(SetPlatformPlanActiveStatus.NotFound);
        }

        plan.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SetPlatformPlanActiveResult(SetPlatformPlanActiveStatus.Success);
    }

    private static PlatformPlanSummaryDto ToSummary(PlatformPlan p) =>
        new(
            p.Id,
            p.Name,
            p.Code,
            p.Description,
            p.PriceMonthly,
            p.MaxSecurityGuards,
            p.MaxSectors,
            p.IsActive,
            p.CreatedAt);

    private static List<string> ValidateCreateInput(CreatePlatformPlanInput input)
    {
        var errors = ValidateCommonInput(input.Name, input.Description, input.PriceMonthly, input.MaxSecurityGuards, input.MaxSectors);

        var normalizedCode = NormalizeCode(input.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode) || normalizedCode.Length > 50)
        {
            errors.Add("Informe um código válido para o plano (até 50 caracteres, letras, números e hífen).");
        }
        else if (!CodePattern().IsMatch(normalizedCode))
        {
            errors.Add("O código do plano deve conter apenas letras minúsculas, números e hífen.");
        }

        return errors;
    }

    private static List<string> ValidateUpdateInput(UpdatePlatformPlanInput input) =>
        ValidateCommonInput(input.Name, input.Description, input.PriceMonthly, input.MaxSecurityGuards, input.MaxSectors);

    private static List<string> ValidateCommonInput(
        string name,
        string? description,
        decimal priceMonthly,
        int maxSecurityGuards,
        int maxSectors)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
        {
            errors.Add("Informe um nome válido para o plano (até 200 caracteres).");
        }

        if (description?.Length > 1000)
        {
            errors.Add("A descrição deve ter no máximo 1000 caracteres.");
        }

        if (priceMonthly < 0)
        {
            errors.Add("O preço mensal não pode ser negativo.");
        }

        if (maxSecurityGuards < 1)
        {
            errors.Add("O limite de seguranças deve ser no mínimo 1.");
        }

        if (maxSectors < 1)
        {
            errors.Add("O limite de setores deve ser no mínimo 1.");
        }

        return errors;
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex CodePattern();
}
