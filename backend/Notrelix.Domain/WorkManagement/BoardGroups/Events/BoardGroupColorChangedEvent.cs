using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupColorChangedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Color OldColor,
    Color NewColor,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
