using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
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
                t.CreatedAt))
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

        var registerInput = new RegisterTenantInput(
            input.TenantName,
            input.AdminName,
            input.AdminEmail.Trim(),
            input.AdminPassword,
            input.AdminPassword);

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
}
