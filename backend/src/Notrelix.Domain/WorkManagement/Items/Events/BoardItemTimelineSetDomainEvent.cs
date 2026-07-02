namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemTimelineSetDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
