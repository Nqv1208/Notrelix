namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationScopeAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    string Scope,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, AddedBy);
