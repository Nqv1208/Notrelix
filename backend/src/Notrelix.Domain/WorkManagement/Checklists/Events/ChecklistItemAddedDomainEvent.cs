using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists.Events;

public sealed record ChecklistItemAddedDomainEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    string Title,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
