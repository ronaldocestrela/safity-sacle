using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Tenancy;

public sealed class PlatformTenantService(
    ApplicationDbContext dbContext,
    ITenantRegistrationService tenantRegistrationService) : IPlatformTenantService
{
    public async Task<IReadOnlyList<PlatformTenantSummaryDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new PlatformTenantSummaryDto(
                t.Id,
                t.Name,
                t.Slug,
                t.IsActive,
                t.CreatedAt,
                (LeadStatusDto)t.LeadStatus,
                t.PlatformPlanId,
                t.PlatformPlan != null ? t.PlatformPlan.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreatePlatformTenantResult> CreateAsync(
        CreatePlatformTenantInput input,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.TenantName))
        {
            validationErrors.Add("Informe o nome da empresa.");
        }

        if (string.IsNullOrWhiteSpace(input.AdminName))
        {
            validationErrors.Add("Informe o nome do administrador.");
        }

        if (!new EmailAddressAttribute().IsValid(input.AdminEmail?.Trim()))
        {
            validationErrors.Add("Informe um e-mail válido.");
        }

        if (string.IsNullOrWhiteSpace(input.AdminPassword))
        {
            validationErrors.Add("Informe a senha do administrador.");
        }

        if (validationErrors.Count > 0)
        {
            return new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.ValidationFailed,
                Errors: validationErrors);
        }

        var planValidation = await ValidatePlanAssignmentAsync(
            input.PlatformPlanId,
            input.LeadStatus,
            cancellationToken);

        if (planValidation is not null)
        {
            return planValidation;
        }

        var adminEmail = input.AdminEmail!.Trim();

        var registerInput = new RegisterTenantInput(
            input.TenantName,
            input.AdminName,
            adminEmail,
            input.AdminPassword,
            input.AdminPassword,
            input.PlatformPlanId,
            input.LeadStatus);

        var result = await tenantRegistrationService.RegisterFromPlatformAsync(registerInput, cancellationToken);

        return result.Status switch
        {
            RegisterTenantStatus.Success => new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.Success,
                result.TenantId,
                result.AdminUserId,
                result.TenantSlug),
            RegisterTenantStatus.AdminEmailAlreadyExists => new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.AdminEmailAlreadyExists),
            RegisterTenantStatus.TenantSlugConflict => new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.TenantSlugConflict),
            RegisterTenantStatus.InvalidPassword => new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.InvalidPassword,
                Errors: result.Errors),
            RegisterTenantStatus.ValidationFailed => new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.ValidationFailed,
                Errors: result.Errors),
            _ => new CreatePlatformTenantResult(CreatePlatformTenantStatus.ValidationFailed),
        };
    }

    public async Task<SetTenantActiveResult> SetActiveAsync(
        Guid tenantId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return new SetTenantActiveResult(SetTenantActiveStatus.NotFound);
        }

        tenant.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SetTenantActiveResult(SetTenantActiveStatus.Success);
    }

    public async Task<UpdateTenantCommercialResult> UpdateCommercialAsync(
        Guid tenantId,
        UpdateTenantCommercialInput input,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return new UpdateTenantCommercialResult(UpdateTenantCommercialStatus.NotFound);
        }

        var planValidation = await ValidatePlanAssignmentAsync(
            input.PlatformPlanId,
            input.LeadStatus,
            cancellationToken,
            currentPlanId: tenant.PlatformPlanId);

        if (planValidation is not null)
        {
            return new UpdateTenantCommercialResult(
                planValidation.Status switch
                {
                    CreatePlatformTenantStatus.PlanNotFound => UpdateTenantCommercialStatus.PlanNotFound,
                    CreatePlatformTenantStatus.PlanInactive => UpdateTenantCommercialStatus.PlanInactive,
                    CreatePlatformTenantStatus.ContractedRequiresPlan => UpdateTenantCommercialStatus.ContractedRequiresPlan,
                    _ => UpdateTenantCommercialStatus.ValidationFailed,
                },
                planValidation.Errors);
        }

        tenant.PlatformPlanId = input.PlatformPlanId;
        tenant.LeadStatus = (LeadStatus)input.LeadStatus;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateTenantCommercialResult(UpdateTenantCommercialStatus.Success);
    }

    private async Task<CreatePlatformTenantResult?> ValidatePlanAssignmentAsync(
        Guid? platformPlanId,
        LeadStatusDto leadStatus,
        CancellationToken cancellationToken,
        Guid? currentPlanId = null)
    {
        if (leadStatus == LeadStatusDto.Contracted && platformPlanId is null)
        {
            return new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.ContractedRequiresPlan,
                Errors: ["Status contratado exige um plano ativo."]);
        }

        if (platformPlanId is null)
        {
            return null;
        }

        var plan = await dbContext.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == platformPlanId.Value, cancellationToken);

        if (plan is null)
        {
            return new CreatePlatformTenantResult(CreatePlatformTenantStatus.PlanNotFound);
        }

        var isSamePlan = currentPlanId == platformPlanId;
        if (!plan.IsActive && !isSamePlan)
        {
            return new CreatePlatformTenantResult(
                CreatePlatformTenantStatus.PlanInactive,
                Errors: ["Somente planos ativos podem ser selecionados."]);
        }

        return null;
    }
}
