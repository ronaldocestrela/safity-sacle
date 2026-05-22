using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Tenancy;

public sealed class TenantRegistrationService(
    ApplicationDbContext dbContext,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager)
    : ITenantRegistrationService
{
    private const int MaxSlugAttempts = 500;

    public async Task<RegisterTenantResult> RegisterAsync(RegisterTenantInput input, CancellationToken cancellationToken = default)
    {
        var trimmedTenantName = input.TenantName.Trim();
        var trimmedAdminName = input.AdminName.Trim();
        var rawEmail = input.AdminEmail.Trim();

        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(trimmedTenantName) || trimmedTenantName.Length > 200)
        {
            validationErrors.Add("Informe um nome válido para a empresa (até 200 caracteres).");
        }

        if (string.IsNullOrWhiteSpace(trimmedAdminName) || trimmedAdminName.Length > 200)
        {
            validationErrors.Add("Informe o nome completo do administrador (até 200 caracteres).");
        }

        if (!new EmailAddressAttribute().IsValid(rawEmail))
        {
            validationErrors.Add("Informe um e-mail válido.");
        }

        if (!string.Equals(input.AdminPassword, input.ConfirmPassword, StringComparison.Ordinal))
        {
            validationErrors.Add("A senha e a confirmação não conferem.");
        }

        if (validationErrors.Count > 0)
        {
            return new RegisterTenantResult(RegisterTenantStatus.ValidationFailed, Errors: validationErrors);
        }

        var existingEmail = await userManager.FindByEmailAsync(rawEmail);
        if (existingEmail is not null)
        {
            return new RegisterTenantResult(RegisterTenantStatus.AdminEmailAlreadyExists);
        }

        await EnsureRoleAsync(IdentitySeeder.Roles.Admin, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var slug = await ResolveUniqueSlugAsync(trimmedTenantName, cancellationToken);
            if (slug is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterTenantResult(RegisterTenantStatus.TenantSlugConflict);
            }

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = trimmedTenantName,
                Slug = slug,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Sectors.Add(new Sector
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Primary",
                Description = null,
                RequiredGuardsPerDay = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            var admin = new AppUser
            {
                UserName = rawEmail,
                Email = rawEmail,
                TenantId = tenant.Id,
                DisplayName = trimmedAdminName,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(admin, input.AdminPassword);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                var passwordCodes = createResult.Errors
                    .Select(e => e.Code)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (passwordCodes.Contains("PasswordTooShort") ||
                    passwordCodes.Contains("PasswordRequiresDigit") ||
                    passwordCodes.Contains("PasswordRequiresLower") ||
                    passwordCodes.Contains("PasswordRequiresUpper") ||
                    passwordCodes.Contains("PasswordRequiresNonAlphanumeric") ||
                    passwordCodes.Contains("InvalidPassword"))
                {
                    return new RegisterTenantResult(
                        RegisterTenantStatus.InvalidPassword,
                        Errors: createResult.Errors.Select(e => e.Description).ToArray());
                }

                if (createResult.Errors.Any(e =>
                        e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                {
                    return new RegisterTenantResult(RegisterTenantStatus.AdminEmailAlreadyExists);
                }

                return new RegisterTenantResult(
                    RegisterTenantStatus.ValidationFailed,
                    Errors: createResult.Errors.Select(e => e.Description).ToArray());
            }

            var roleResult = await userManager.AddToRoleAsync(admin, IdentitySeeder.Roles.Admin);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(admin);
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterTenantResult(
                    RegisterTenantStatus.ValidationFailed,
                    Errors: roleResult.Errors.Select(e => e.Description).ToArray());
            }

            await transaction.CommitAsync(cancellationToken);

            return new RegisterTenantResult(
                RegisterTenantStatus.Success,
                tenant.Id,
                admin.Id,
                tenant.Slug);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var create = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!create.Succeeded && !await roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException($"Unable to ensure role '{roleName}' exists.");
        }
    }

    private async Task<string?> ResolveUniqueSlugAsync(string tenantName, CancellationToken cancellationToken)
    {
        var baseSlug = TenantSlugNormalizer.ToSlugBase(tenantName);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "tenant";
        }

        for (var i = 0; i < MaxSlugAttempts; i++)
        {
            var candidate = i == 0 ? baseSlug : $"{baseSlug}-{i + 1}";

            candidate = TenantSlugNormalizer.Clamp(candidate, 100);
            var taken = await dbContext.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Slug == candidate, cancellationToken);

            if (!taken)
            {
                return candidate;
            }
        }

        return null;
    }
}
