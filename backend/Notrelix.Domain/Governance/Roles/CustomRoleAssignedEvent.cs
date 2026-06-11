using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Roles;

public sealed record CustomRoleAssignedEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
