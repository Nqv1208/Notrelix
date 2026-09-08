namespace Notrelix.Application.Events.Automation;

/// <summary>
/// Durable intent to dispatch an accepted automation execution to n8n.
/// Persisted atomically with <see cref="Domain.Automation.Executions.AutomationExecution"/>
/// in the same outbox transaction; the n8n HTTP call happens in the consumer after commit.
/// <see cref="ExecutionId"/> is the stable external idempotency/correlation identity.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("automation.n8n-dispatch-requested", Version = 1)]
public sealed record N8nDispatchRequestedV1(
    Guid EventId,
    Guid ExecutionId,
    Guid RuleId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null)
    : IntegrationEvent(
        EventId,
        "automation.n8n-dispatch-requested",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        actorUserId: null,
        causationId: CausationId,
        occurredAt: OccurredAt);
