using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterSoftDeletedEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
