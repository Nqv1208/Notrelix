namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemCompletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? CompletedAt,
    Guid CompletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CompletedBy);
