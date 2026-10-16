using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Modules.Common.Behaviors;

namespace Modules;

public static class DependencyInjection
{
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
