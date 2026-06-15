using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemTimelineSetEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
