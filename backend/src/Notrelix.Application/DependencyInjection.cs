using System.Reflection;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Pipeline;
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
        // Tenant context bootstrap
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBootstrapBehavior<,>));
        // Post-commit: dispatch cache invalidations + realtime after commit succeeds
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PostCommitActionBehavior<,>));
        // Cache-first for ICacheableQuery (before RLS/TXN — cache hits skip DB entirely)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheBehavior<,>));
        // RLS session: set PostgreSQL session vars for DB-level security
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RlsSessionBehavior<,>));
        // Transactional: begin/commit transaction, SaveChanges for commands
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionalBehavior<,>));
        // Authorization (IRequirePermission only)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        // Idempotency for IIdempotentRequest
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        // Entitlement check for IRequireEntitlement
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EntitlementBehavior<,>));
        // Cache invalidation after successful handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        // Realtime publish after successful handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RealtimeBehavior<,>));

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
