using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterSortsUpdatedEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
