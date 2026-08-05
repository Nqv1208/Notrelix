namespace Notrelix.Application.Features.Governance.DTOs;

public record ResourcePermissionDto(
    Guid Id,
    Guid WorkspaceId,
    string ResourceKind,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    Guid? GrantedBy,
    bool IsRevoked,
    DateTimeOffset? RevokedAt
);
