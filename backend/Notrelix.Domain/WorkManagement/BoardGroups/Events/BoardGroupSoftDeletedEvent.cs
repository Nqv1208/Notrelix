using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupSoftDeletedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
