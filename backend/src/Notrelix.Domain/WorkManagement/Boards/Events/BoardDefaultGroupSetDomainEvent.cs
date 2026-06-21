using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardDefaultGroupSetDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
