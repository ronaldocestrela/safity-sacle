using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SafetyScale.Application.Common.Behaviors;

namespace SafetyScale.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<AssemblyReference>());

        services.AddValidatorsFromAssemblyContaining<AssemblyReference>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
