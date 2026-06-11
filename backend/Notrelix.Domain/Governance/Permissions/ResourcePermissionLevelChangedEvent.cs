using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Permissions;

public sealed record ResourcePermissionLevelChangedEvent(
    Guid WorkspaceId,
    Guid PermissionId,
    PermissionLevel OldLevel,
    PermissionLevel NewLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
