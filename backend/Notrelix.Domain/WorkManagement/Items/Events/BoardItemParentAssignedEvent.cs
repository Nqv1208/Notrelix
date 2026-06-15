using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemParentAssignedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid? ParentItemId,
    int ItemLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
