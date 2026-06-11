using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups;

public sealed record BoardGroupSoftDeletedEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
