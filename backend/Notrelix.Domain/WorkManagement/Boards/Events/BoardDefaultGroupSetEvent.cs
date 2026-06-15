using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardDefaultGroupSetEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
