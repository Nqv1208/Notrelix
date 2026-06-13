using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemToggledEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    bool IsDone,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
