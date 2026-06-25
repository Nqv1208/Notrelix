namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionExpiredDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid ExpiredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ExpiredBy);
