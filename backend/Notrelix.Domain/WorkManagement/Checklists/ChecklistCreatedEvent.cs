using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists;

public sealed record ChecklistCreatedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid ChecklistId,
    string Title,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
