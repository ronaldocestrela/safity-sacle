using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure;

public static class ServiceProviderExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, bool isDevelopment)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync(isDevelopment);
    }
}
