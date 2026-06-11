using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups;

public sealed record BoardGroupReorderedEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid BoardId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
