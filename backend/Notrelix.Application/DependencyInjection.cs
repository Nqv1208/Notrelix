using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Hosting;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;

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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        // FluentValidation - auto register all validators
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IWorkspacePermissionService, WorkspacePermissionService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IN8nSignatureService, N8nSignatureService>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
