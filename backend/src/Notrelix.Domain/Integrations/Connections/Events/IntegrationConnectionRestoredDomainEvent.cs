namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-restored")]
public sealed record IntegrationConnectionRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
