using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Infrastructure.Authentication;

public sealed class SecurityGuardInviteService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailQueueService emailQueueService,
    IOptions<PublicUrlsOptions> publicUrlsOptions,
    ITenantExecutionContext tenantExecutionContext) : ISecurityGuardInviteService
{
    public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim();
        var existing = await userManager.FindByEmailAsync(normalized);
        return existing is null;
    }

    public async Task InviteAsync(
        Guid securityGuardId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantExecutionContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required to invite a security guard user.");

        var normalizedEmail = email.Trim();
        if (!await IsEmailAvailableAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{normalizedEmail}' is already registered.");
        }

        await EnsureRoleAsync(IdentitySeeder.Roles.SecurityGuard, cancellationToken);

        var user = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            UserKind = UserKind.Tenant,
            TenantId = tenantId,
            DisplayName = displayName.Trim(),
            SecurityGuardId = securityGuardId,
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create invited user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, IdentitySeeder.Roles.SecurityGuard);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException(
                $"Failed to assign SecurityGuard role: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var setPasswordLink = SetPasswordLinkBuilder.Build(
            publicUrlsOptions.Value.WebBaseUrl,
            user.Id,
            token);

        var message = SecurityGuardInviteMessageFactory.Create(normalizedEmail, displayName.Trim(), setPasswordLink);
        await emailQueueService.EnqueueAsync(message, cancellationToken);
    }

    private async Task EnsureRoleAsync(string role, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(role));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to ensure role '{role}'.");
        }
    }
}
