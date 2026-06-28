namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionReauthorizedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
