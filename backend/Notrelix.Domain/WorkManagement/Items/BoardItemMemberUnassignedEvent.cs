using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemMemberUnassignedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid UnassignedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
