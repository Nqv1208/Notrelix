using System.Reflection;
using Notrelix.Application.Common.Behaviors;

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

        // MediatR Pipeline Behaviors (outermost -> innermost)
        // Outer zone: pre-DB
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApplicationTracingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBootstrapBehavior<,>));
        // Post-commit scope: wraps DB scope, flushes side effects after commit
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PostCommitScopeBehavior<,>));
        // Public cache: cache-first for shared/public queries (before DB scope)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublicCacheBehavior<,>));
        // DB/RLS/Transaction boundary: single scope for RLS + transaction + SaveChanges
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DbRequestScopeBehavior<,>));
        // Inner zone: inside DB/RLS scope
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SubscriptionGateBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FeatureGateBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        // Post-commit enqueue: enqueues side effects from within DB scope (runs after handler)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PostCommitEnqueueBehavior<,>));
        // Authorized cache: runs inside DB/RLS scope, after auth, for private data
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizedCacheBehavior<,>));

        // FluentValidation - auto register all validators
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped(typeof(IAuthorizationDecisionStore), sp => sp.GetRequiredService<IPermissionService>());
        services.AddScoped<IWorkspacePermissionService, WorkspacePermissionService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionEvaluator, PermissionService>();
        services.AddSingleton<IN8nSignatureService, N8nSignatureService>();

        // Execution context (scoped per request)
        services.AddScoped<IExecutionContextAccessor, Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextReader>(sp => sp.GetRequiredService<IExecutionContextAccessor>());

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
