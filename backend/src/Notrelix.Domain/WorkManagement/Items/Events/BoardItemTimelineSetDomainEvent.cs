namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-timeline-set")]
public sealed record BoardItemTimelineSetDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PreviousStartedAt = null,
    DateTimeOffset? PreviousDueAt = null
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
