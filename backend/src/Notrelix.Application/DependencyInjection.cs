using System.Reflection;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Services;

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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractGuardBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TokenValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBootstrapBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SystemOperationAuditBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceScopeBehavior<,>));
        // Post-commit scope: wraps DB scope, flushes side effects after commit
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PostCommitScopeBehavior<,>));
        // Public cache: cache-first for shared/public queries (before DB scope)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublicCacheBehavior<,>));
        // DB/RLS/Transaction boundary: single scope for RLS + transaction + SaveChanges
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DbRequestScopeBehavior<,>));
        // Inner zone: inside DB/RLS scope
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(VerifiedEmailBehavior<,>));
        // Concurrency: version check for IExpectedVersionRequest (inside DB/RLS scope, after auth)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyBehavior<,>));
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

        // Idempotency services
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();

        // Integration event collector (scoped per request)
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();

        // Auth session issuer
        services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();
        services.AddScoped<IEmailVerificationTokenIssuer, EmailVerificationTokenIssuer>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
