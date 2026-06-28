namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationScopeRemovedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Scope,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RemovedBy);
