using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles.Events;

public sealed record CustomRoleArchivedEvent(
    Guid WorkspaceId,
    Guid RoleId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
