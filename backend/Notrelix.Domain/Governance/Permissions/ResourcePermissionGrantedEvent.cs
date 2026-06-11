using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Permissions;

public sealed record ResourcePermissionGrantedEvent(
    Guid WorkspaceId,
    Guid PermissionId,
    ResourceType ResourceType,
    Guid ResourceId,
    PermissionSubjectType SubjectType,
    Guid SubjectId,
    PermissionLevel Level,
    Guid GrantedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
