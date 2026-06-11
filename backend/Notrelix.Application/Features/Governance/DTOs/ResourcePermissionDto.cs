namespace Notrelix.Application.Features.Governance.DTOs;

public record ResourcePermissionDto(
    Guid Id,
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    Guid? GrantedBy,
    DateTime? ExpiresAt,
    bool IsRevoked,
    DateTime? RevokedAt
);
