using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Roles;

public sealed record CustomRoleRevokedEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid MemberId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
