using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Roles;

public sealed record CustomRoleCreatedEvent(
    Guid RoleId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
