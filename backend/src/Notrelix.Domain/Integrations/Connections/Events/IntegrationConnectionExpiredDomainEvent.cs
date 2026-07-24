namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-expired")]
public sealed record IntegrationConnectionExpiredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid ExpiredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
