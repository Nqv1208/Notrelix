using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists;

public sealed record ChecklistItemRemovedEvent(
    Guid WorkspaceId,
    Guid ChecklistId,
    Guid ItemId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
