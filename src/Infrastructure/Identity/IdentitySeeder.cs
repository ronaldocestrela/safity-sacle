using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Identity;

public class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    ApplicationDbContext dbContext,
    IOptions<BootstrapUserOptions> bootstrapUserOptions,
    ILogger<IdentitySeeder> logger)
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Supervisor = "Supervisor";
        public const string SecurityGuard = "SecurityGuard";
    }

    public static class PlatformRoles
    {
        public const string Owner = "PlatformOwner";
        public const string Admin = "PlatformAdmin";
        public const string Support = "PlatformSupport";

        public static readonly string[] All = [Owner, Admin, Support];
    }

    public async Task SeedAsync(bool isDevelopment)
    {
        var tenantRoles = new[] { Roles.Admin, Roles.Supervisor, Roles.SecurityGuard };
        var allRoles = tenantRoles.Concat(PlatformRoles.All);

        foreach (var role in allRoles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                logger.LogWarning("Failed to create role {Role}", role);
            }
        }

        var defaultTenantId = await EnsureDefaultTenantAsync();
        await AssignMissingTenantToUsersAsync(defaultTenantId);
        await SeedBootstrapPlatformUserAsync();

        if (!isDevelopment)
        {
            return;
        }

        var developmentAdmins = new[]
        {
            (Email: "admin@safetyscale.local", Password: "Admin@12345"),
            (Email: "admin@local.com", Password: "Mudar@13")
        };

        foreach (var (email, password) in developmentAdmins)
        {
            var existingAdmin = await userManager.FindByEmailAsync(email);
            if (existingAdmin is not null)
            {
                if (!await userManager.IsInRoleAsync(existingAdmin, Roles.Admin))
                {
                    await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                }

                continue;
            }

            var user = new AppUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                UserKind = UserKind.Tenant,
                TenantId = defaultTenantId,
                DisplayName = email.Split('@')[0],
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Failed to create development admin user {Email}.", email);
                continue;
            }

            await userManager.AddToRoleAsync(user, Roles.Admin);
        }

        await EnsureSupervisorUserAsync(defaultTenantId);
    }

    private async Task SeedBootstrapPlatformUserAsync()
    {
        var options = bootstrapUserOptions.Value;
        var email = options.Email?.Trim();
        var password = options.Password;
        var displayName = string.IsNullOrWhiteSpace(options.DisplayName)
            ? email?.Split('@')[0] ?? "Platform Admin"
            : options.DisplayName.Trim();
        var role = string.IsNullOrWhiteSpace(options.Role)
            ? PlatformRoles.Owner
            : options.Role.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Bootstrap platform user skipped: BootstrapUser__Email and BootstrapUser__Password must be set.");
            return;
        }

        if (!PlatformRoles.All.Contains(role, StringComparer.Ordinal))
        {
            logger.LogWarning(
                "Bootstrap platform user skipped: role {Role} is not a valid platform role.",
                role);
            return;
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            var createRole = await roleManager.CreateAsync(new IdentityRole(role));
            if (!createRole.Succeeded)
            {
                logger.LogWarning("Failed to ensure bootstrap role {Role}.", role);
                return;
            }
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.UserKind != UserKind.Platform)
            {
                logger.LogWarning(
                    "Bootstrap platform user skipped: email {Email} already belongs to a tenant user.",
                    email);
                return;
            }

            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new AppUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            UserKind = UserKind.Platform,
            TenantId = null,
            DisplayName = displayName,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Failed to create bootstrap platform user {Email}: {Errors}",
                email,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Bootstrap platform user {Email} created with role {Role}.", email, role);
    }

    private async Task<Guid> EnsureDefaultTenantAsync()
    {
        const string slug = "default";
        var existing = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (existing is not null)
        {
            await EnsurePrimarySectorAsync(existing.Id);
            return existing.Id;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Created default tenant {TenantId}", tenant.Id);
        await EnsurePrimarySectorAsync(tenant.Id);
        return tenant.Id;
    }

    private async Task EnsurePrimarySectorAsync(Guid tenantId)
    {
        if (await dbContext.Sectors
                .IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Name == "Primary"))
        {
            return;
        }

        dbContext.Sectors.Add(new Sector
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Primary",
            Description = null,
            RequiredGuardsPerDay = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }

    private async Task AssignMissingTenantToUsersAsync(Guid tenantId)
    {
        var users = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.UserKind == UserKind.Tenant &&
                        (u.TenantId == null || u.TenantId == Guid.Empty))
            .ToListAsync();

        foreach (var user in users)
        {
            user.TenantId = tenantId;
        }

        if (users.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task EnsureSupervisorUserAsync(Guid defaultTenantId)
    {
        const string email = "supervisor@safetyscale.local";
        const string password = "Supervisor@12345";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.Supervisor))
            {
                await userManager.AddToRoleAsync(existing, Roles.Supervisor);
            }

            return;
        }

        var user = new AppUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            UserKind = UserKind.Tenant,
            TenantId = defaultTenantId,
            DisplayName = "Supervisor dev",
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create development supervisor user {Email}.", email);
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.Supervisor);
    }
}
