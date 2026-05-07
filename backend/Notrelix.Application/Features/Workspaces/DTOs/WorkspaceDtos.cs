namespace Notrelix.Application.Features.Workspaces.DTOs;

public record WorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsPersonal,
    string Plan,
    string? IconType,
    string? IconValue,
    string? CoverUrl,
    bool IsArchived,
    int MemberCount,
    DateTime CreatedAt
);

public record WorkspaceMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);

public record WorkspaceInvitationDto(
    Guid Id,
    string Email,
    string Role,
    DateTime ExpiresAt,
    bool IsAccepted,
    DateTime CreatedAt
);
