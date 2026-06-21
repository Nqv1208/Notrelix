using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterVisibilityUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid FilterId,
    Guid BoardId,
    SavedFilterVisibility Visibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
