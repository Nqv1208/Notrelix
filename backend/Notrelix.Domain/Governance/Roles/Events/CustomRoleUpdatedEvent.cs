using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleUpdatedEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
