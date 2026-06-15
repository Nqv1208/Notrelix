using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistSoftDeletedEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
