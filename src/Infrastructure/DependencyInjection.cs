using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Persistence;
using SafetyScale.Infrastructure.Persistence.Repositories;
using SafetyScale.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SafetyScale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=safetyscale.db";

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<ITenantExecutionContext, TenantExecutionContext>();

        services.AddDbContext<ApplicationDbContext>((_, options) =>
            options.UseSqlite(connectionString));

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantRegistrationService, TenantRegistrationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IdentitySeeder>();
        services.AddScoped<ISecurityGuardRepository, SecurityGuardRepository>();
        services.AddScoped<IUnavailableDayRepository, UnavailableDayRepository>();
        services.AddScoped<IMonthlyScheduleRepository, MonthlyScheduleRepository>();

        return services;
    }
}
