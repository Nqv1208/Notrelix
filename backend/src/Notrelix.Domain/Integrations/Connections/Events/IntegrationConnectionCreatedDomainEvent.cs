namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-created")]
public sealed record IntegrationConnectionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    IntegrationProvider Provider,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
