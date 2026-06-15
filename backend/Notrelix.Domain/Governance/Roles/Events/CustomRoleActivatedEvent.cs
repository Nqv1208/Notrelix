using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleActivatedEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActivatedBy);
