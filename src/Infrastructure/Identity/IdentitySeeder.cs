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

        const string adminEmail = "admin@safetyscale.local";
        const string adminPassword = "Admin@12345";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            return;
        }

        var user = new AppUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, adminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create development admin user.");
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.Admin);
    }
}
