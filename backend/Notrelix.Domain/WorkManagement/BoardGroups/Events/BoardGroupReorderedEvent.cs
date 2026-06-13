using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupReorderedEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid BoardId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
