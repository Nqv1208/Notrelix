using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Permissions;

public sealed record ResourcePermissionRevokedEvent(
    Guid WorkspaceId,
    Guid PermissionId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
