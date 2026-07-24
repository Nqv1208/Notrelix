namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-reauthorized")]
public sealed record IntegrationConnectionReauthorizedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
