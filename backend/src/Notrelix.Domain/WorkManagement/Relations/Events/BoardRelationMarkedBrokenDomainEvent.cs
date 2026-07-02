namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationMarkedBrokenDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid MarkedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, MarkedBy);
