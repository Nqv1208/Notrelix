using System.Reflection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Features.Accounts.Members.Services;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Accounts.Public.Commands;
using Notrelix.Application.Features.Accounts.Public.Queries;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.Commands;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Services;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Services;
using Notrelix.Application.Common.Requests.Execution;

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
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(assembly));

        // MediatR Pipeline Behaviors (outermost -> innermost)
        // Outer zone: pre-DB
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApplicationTracingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        // FluentValidation - auto register all validators
        services.AddValidatorsFromAssembly(assembly);

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddSingleton<IN8nSignatureService, N8nSignatureService>();
        services.AddScoped<Notrelix.Application.Features.Automation.Events.N8nAutomationRuleEvaluator>();

        // Execution context (scoped per request)
        services.AddScoped<IExecutionContextAccessor, Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextReader>(sp => sp.GetRequiredService<IExecutionContextAccessor>());

        // Idempotency services
        services.AddOptions<IdempotencyOptions>()
            .Bind(builder.Configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();

        // Scoped execution context: one instance is exposed as both the read and
        // the write contract so the transport can bind the key and the pipeline
        // can require it.
        services.AddScoped<IIdempotencyExecutionContext, IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            (IIdempotencyExecutionContextWriter)sp.GetRequiredService<IIdempotencyExecutionContext>());

        // Integration event collector (scoped per request)
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        foreach (var mapper in assembly.GetTypes().Where(type => type is { IsAbstract: false, IsClass: true }))
        {
            foreach (var contract in mapper.GetInterfaces().Where(type =>
                         type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRealtimeChangeMapper<,>)))
            {
                services.AddScoped(contract, mapper);
            }
        }

        // Auth session issuer
        services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();
        services.AddScoped<IEmailVerificationTokenIssuer, EmailVerificationTokenIssuer>();

        // Identity security: canonical step-up verification + shared MFA code verification
        services.AddScoped<ISecurityStepUpService, SecurityStepUpService>();
        services.AddScoped<IMfaCodeVerifier, MfaCodeVerifier>();

        // Accounts-owned onboarding provisioning (spec 5.2)
        services.AddScoped<IAccountProvisioningService, AccountProvisioningService>();

        // Producer-owned public semantic facts (real consumers only)
        services.AddScoped<IIdentityUserFacts, IdentityUserFactsProvider>();
        services.AddScoped<IAccountMembershipFacts, AccountMembershipFactsProvider>();

        // Producer-owned public target actions (Accounts membership mutation)
        services.AddScoped<IAccountMembershipActions, AccountMembershipActions>();

        // Producer-owned public target actions (WorkManagement item mutations)
        services.AddScoped<MoveBoardItemUseCase>();
        services.AddScoped<IWorkItemActions, WorkItemActions>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
