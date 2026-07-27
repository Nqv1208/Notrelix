namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-error-recorded")]
public sealed record IntegrationConnectionErrorRecordedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    string ErrorDetail,
    Guid RecordedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);