using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// AUT-020: thin inbound adapters for the Workspaces-owned member standing facts.
/// Each extracts the authoritative scope/user from the message and delegates to
/// the Application-owned <see cref="AutomationExecutorLifecycle"/>, which stops the
/// Active rules that member runs. The consumers own no Automation semantics, and
/// Workspaces storage is never read or written.
/// </summary>
[OrderingNotApplicable("Disabling a member's Active rules is an idempotent per-message convergence (a rule already disabled is a no-op); execution-time re-authorization is the authority, so no later message supersedes an earlier one.")]
public sealed class WorkspaceMemberRemovedAutomationConsumer : IConsumer<WorkspaceMemberRemovedIntegrationEvent>
{
    private readonly AutomationExecutorLifecycle _lifecycle;

    public WorkspaceMemberRemovedAutomationConsumer(AutomationExecutorLifecycle lifecycle) => _lifecycle = lifecycle;

    public Task Consume(ConsumeContext<WorkspaceMemberRemovedIntegrationEvent> context) =>
        _lifecycle.OnMemberRemovedAsync(
            context.Message.AccountId ?? throw MissingScope(),
            context.Message.WorkspaceId ?? throw MissingScope(),
            context.Message.UserId,
            context.CancellationToken);

    private static InvalidOperationException MissingScope() =>
        new("The member fact carried no tenant scope; the tenant envelope is authoritative.");
}

[OrderingNotApplicable("Disabling a member's Active rules is an idempotent per-message convergence (a rule already disabled is a no-op); execution-time re-authorization is the authority, so no later message supersedes an earlier one.")]
public sealed class WorkspaceMemberSuspendedAutomationConsumer : IConsumer<WorkspaceMemberSuspendedIntegrationEvent>
{
    private readonly AutomationExecutorLifecycle _lifecycle;

    public WorkspaceMemberSuspendedAutomationConsumer(AutomationExecutorLifecycle lifecycle) => _lifecycle = lifecycle;

    public Task Consume(ConsumeContext<WorkspaceMemberSuspendedIntegrationEvent> context) =>
        _lifecycle.OnMemberSuspendedAsync(
            context.Message.AccountId ?? throw MissingScope(),
            context.Message.WorkspaceId ?? throw MissingScope(),
            context.Message.UserId,
            context.CancellationToken);

    private static InvalidOperationException MissingScope() =>
        new("The member fact carried no tenant scope; the tenant envelope is authoritative.");
}

[OrderingNotApplicable("Disabling a member's Active rules on a demotion is an idempotent per-message convergence; execution-time re-authorization is the authority, so no later message supersedes an earlier one.")]
public sealed class WorkspaceMemberRoleChangedAutomationConsumer : IConsumer<WorkspaceMemberRoleChangedIntegrationEvent>
{
    private readonly AutomationExecutorLifecycle _lifecycle;

    public WorkspaceMemberRoleChangedAutomationConsumer(AutomationExecutorLifecycle lifecycle) => _lifecycle = lifecycle;

    public Task Consume(ConsumeContext<WorkspaceMemberRoleChangedIntegrationEvent> context) =>
        _lifecycle.OnMemberRoleChangedAsync(
            context.Message.AccountId ?? throw MissingScope(),
            context.Message.WorkspaceId ?? throw MissingScope(),
            context.Message.UserId,
            context.Message.IsDemotion,
            context.CancellationToken);

    private static InvalidOperationException MissingScope() =>
        new("The member fact carried no tenant scope; the tenant envelope is authoritative.");
}
