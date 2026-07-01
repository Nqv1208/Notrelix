namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RevokedBy);
