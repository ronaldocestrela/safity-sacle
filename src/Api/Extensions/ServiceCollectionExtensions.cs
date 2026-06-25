using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SafetyScale.Api.Authorization;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Identity;
using System.Text;
using System.Text.Json;

namespace SafetyScale.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
        services.AddFluentValidationAutoValidation();
        services.AddEndpointsApiExplorer();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.PlatformManagement,
                policy => policy.RequireRole(
                    IdentitySeeder.PlatformRoles.Owner,
                    IdentitySeeder.PlatformRoles.Admin));

            options.AddPolicy(
                AuthorizationPolicies.PlatformRead,
                policy => policy.RequireRole(
                    IdentitySeeder.PlatformRoles.Owner,
                    IdentitySeeder.PlatformRoles.Admin,
                    IdentitySeeder.PlatformRoles.Support));
        });
        services.AddSwaggerGen();

        var corsOrigins = CorsConfigurationHelper.ResolveAllowedOrigins(configuration);
        if (corsOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        return services;
    }
}
