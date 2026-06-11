using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Permissions;

public sealed record FieldPermissionRevokedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType SubjectType,
    Guid SubjectId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
