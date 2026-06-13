using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupRestoredEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
