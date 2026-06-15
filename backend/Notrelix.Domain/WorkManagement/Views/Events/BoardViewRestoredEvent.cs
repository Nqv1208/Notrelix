using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewRestoredEvent(
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
