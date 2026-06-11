using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemMovedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid OldGroupId,
    Guid NewGroupId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
