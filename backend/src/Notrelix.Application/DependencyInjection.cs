using System.Reflection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Accounts.Public.Domains;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Application.Common.Integrations.N8n;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Billing.Capacity;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
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
        services.AddOptions<Notrelix.Application.Features.Automation.AutomationGuardOptions>()
            .Bind(builder.Configuration.GetSection(
                Notrelix.Application.Features.Automation.AutomationGuardOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<Notrelix.Application.Features.Automation.AutomationGuardOptions>,
            Notrelix.Application.Features.Automation.AutomationGuardOptionsValidator>();


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

        // Per-request compensation for external effects (object storage) that cannot join the DB transaction.
        services.AddScoped<IRequestRollbackCompensations, RequestRollbackCompensations>();

        // Attachment upload policy (size cap, per-item cap, blocked types): validated at startup.
        services.AddOptions<Notrelix.Application.Features.Collaboration.Attachments.AttachmentUploadOptions>()
            .Bind(builder.Configuration.GetSection(
                Notrelix.Application.Features.Collaboration.Attachments.AttachmentUploadOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<Notrelix.Application.Features.Collaboration.Attachments.AttachmentUploadOptions>,
            Notrelix.Application.Features.Collaboration.Attachments.AttachmentUploadOptionsValidator>();

        // Scoped execution context: one instance is exposed as both the read and
        // the write contract so the transport can bind the key and the pipeline
        // can require it.
        services.AddScoped<IIdempotencyExecutionContext, IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            (IIdempotencyExecutionContextWriter)sp.GetRequiredService<IIdempotencyExecutionContext>());

        // Integration event collector (scoped per request)
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddSingleton<Notrelix.Application.Features.WorkManagement.Ordering.WorkOrderingMetrics>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Ordering.WorkPlacementResolver>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Ordering.WorkOrderingRebalancer>();
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
        services.AddScoped<IAccountProvisioningActions, AccountProvisioningService>();

        // Producer-owned public semantic facts (real consumers only)
        services.AddScoped<IIdentityUserFacts, IdentityUserFactsProvider>();
        services.AddScoped<Notrelix.Application.Features.Identity.Public.DefaultWorkspace.IIdentityDefaultWorkspace,
            Notrelix.Application.Features.Identity.Profiles.Services.IdentityDefaultWorkspaceProvider>();
        services.AddScoped<IAccountMembershipFacts, AccountMembershipFactsProvider>();
        services.AddScoped<IAccountDomainFacts, AccountDomainFactsProvider>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Public.Members.IMemberEmailDisclosure,
            Notrelix.Application.Features.Workspaces.Members.Services.MemberEmailDisclosure>();

        // Producer-owned public target actions (Accounts membership mutation)
        services.AddScoped<IAccountMembershipActions, AccountMembershipActions>();

        services.AddScoped<Notrelix.Application.Features.Workspaces.Invitations.Services.InvitationInitialEntries>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Invitations.Services.InvitationAcceptanceService>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Invitations.Services.InvitationTokenScope>();
        services.AddScoped<Notrelix.Application.Features.Identity.Public.InvitedRegistration.IInvitedUserRegistrationActions,
            Notrelix.Application.Features.Identity.Registration.Services.InvitedUserRegistrationActions>();

        // Producer-owned public target actions (WorkManagement item mutations)
        services.AddScoped<MoveBoardItemUseCase>();
        services.AddScoped<IWorkItemActions, WorkItemActions>();
        services.AddScoped<IWorkItemActionAuthorizer, WorkItemActionAuthorizer>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceTargetAuthorizer,
            Notrelix.Application.Features.Workspaces.Spaces.Services.SpaceTargetAuthorizer>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceContentReader,
            Notrelix.Application.Features.Workspaces.Spaces.Services.SpaceContentReader>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceReachReader,
            Notrelix.Application.Features.Workspaces.Spaces.Services.SpaceReachReader>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Boards.Services.BoardAccessChangeLog>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement,
            Notrelix.Application.Features.WorkManagement.Boards.Services.BoardSpacePlacement>();
        services.AddScoped<Notrelix.Application.Features.Documents.Pages.Services.IPageSpacePlacement,
            Notrelix.Application.Features.Documents.Pages.Services.PageSpacePlacement>();
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IResourceAccessSummary,
            Notrelix.Application.Features.Governance.Authorization.ResourceAccessSummaryService>();
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IBoardSharingSummary>(sp =>
            (Notrelix.Application.Features.Governance.Authorization.ResourceAccessSummaryService)
            sp.GetRequiredService<Notrelix.Application.Features.Governance.Public.Access.IResourceAccessSummary>());
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IResourceAccessChangeRecorder,
            Notrelix.Application.Features.Governance.AccessChanges.ResourceAccessChangeRecorder>();

        // Producer-owned public projection source (WorkManagement placement facts)
        services.AddScoped<IWorkItemProjectionSource, WorkItemProjectionSourceService>();

        // Producer-owned public capability surface (Billing)
        services.AddScoped<IBillingCapabilityFacts, BillingCapabilityFactsProvider>();

        // Producer-owned public capacity action (Billing hard-quota consume/release)
        services.AddScoped<IBillingCapacityActions, BillingCapacityActions>();

        // Automation-owned dispatch use cases (process progression)
        services.AddScoped<N8nDispatchUseCase>();
        services.AddScoped<global::Notrelix.Application.Features.Automation.Rules.Services.AutomationExecutorAuthority>();
        services.AddScoped<global::Notrelix.Application.Features.Automation.Rules.Services.AutomationExecutorLifecycle>();
        services.AddScoped<
            global::Notrelix.Application.Features.Automation.Executions.Services.AutomationMoveItemUseCase>();

        // Integrations-owned calendar webhook processing seam (provider-neutral
        // processing under the derived tenant — Wave E of TAC v2.6 closure).
        services.AddScoped<
            Notrelix.Application.Features.Integrations.Calendar.Processing.ICalendarWebhookProcessingUseCase,
            Notrelix.Application.Features.Integrations.Calendar.Processing.CalendarWebhookProcessingUseCase>();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);
    }
}
