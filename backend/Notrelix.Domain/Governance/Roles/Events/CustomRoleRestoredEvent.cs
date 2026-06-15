using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleRestoredEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
