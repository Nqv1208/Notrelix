namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionRevokedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RevokedBy);
