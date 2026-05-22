using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Identity;

public class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<IdentitySeeder> logger)
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Supervisor = "Supervisor";
    }

    public async Task SeedAsync(bool isDevelopment)
    {
        var roles = new[] { Roles.Admin, Roles.Supervisor };

        foreach (var role in roles)
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
            .Where(u => u.TenantId == Guid.Empty)
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
