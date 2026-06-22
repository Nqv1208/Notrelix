using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Hosting;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(assembly));

        // MediatR Pipeline Behaviors (outermost → innermost)
        // ExceptionMapping wraps all behaviors to catch unhandled exceptions
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        // Logging
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        // Validation
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // Workspace context resolution
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(WorkspaceContextBehavior<,>));
        // Authorization (IRequirePermission only)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        // Cache-first for ICacheableQuery
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheBehavior<,>));
        // Idempotency for IIdempotentRequest
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        // Entitlement check for IRequireEntitlement
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EntitlementBehavior<,>));
        // Cache invalidation after successful handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        // Realtime publish after successful handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RealtimeBehavior<,>));
        // Transaction wraps handler, runs SaveChanges before CacheInvalidation/Realtime run
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // FluentValidation - auto register all validators
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IWorkspacePermissionService, WorkspacePermissionService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionEvaluator, PermissionService>();
        services.AddSingleton<IN8nSignatureService, N8nSignatureService>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
