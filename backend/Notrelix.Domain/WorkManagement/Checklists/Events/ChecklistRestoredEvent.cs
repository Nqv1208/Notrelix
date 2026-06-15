using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistRestoredEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
