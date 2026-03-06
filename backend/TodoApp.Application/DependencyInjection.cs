using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Hosting;
using TodoApp.Application.Common.Behaviors;

namespace Microsoft.Extensions.DependencyInjection;

// Application Layer Dependency Injection
public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(assembly));

        // MediatR Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // FluentValidation - auto register all validators
        services.AddValidatorsFromAssembly(assembly);

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
