using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace SafetyScale.Infrastructure.Identity;

public class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
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
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Failed to create development admin user {Email}.", email);
                continue;
            }

            await userManager.AddToRoleAsync(user, Roles.Admin);
        }

        await EnsureSupervisorUserAsync();
    }

    private async Task EnsureSupervisorUserAsync()
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
