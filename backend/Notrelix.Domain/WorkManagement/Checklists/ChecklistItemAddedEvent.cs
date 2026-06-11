using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists;

public sealed record ChecklistItemAddedEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    string Title,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
