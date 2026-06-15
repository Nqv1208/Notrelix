using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormRestoredEvent(
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
