namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    IntegrationProvider Provider,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
