using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemCompletedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset? CompletedAt,
    Guid CompletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CompletedBy);
