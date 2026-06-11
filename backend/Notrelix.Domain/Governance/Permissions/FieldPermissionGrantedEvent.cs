using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Permissions;

public sealed record FieldPermissionGrantedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    PermissionSubjectType SubjectType,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
