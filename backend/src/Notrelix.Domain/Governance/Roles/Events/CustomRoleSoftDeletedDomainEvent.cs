using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
