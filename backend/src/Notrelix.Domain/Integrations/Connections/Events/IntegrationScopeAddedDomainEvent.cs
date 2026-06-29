namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationScopeAddedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Scope,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, AddedBy);
