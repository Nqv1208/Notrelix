namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemTimelineSetDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
