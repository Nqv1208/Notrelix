namespace Notrelix.Application.Features.Workspaces.DTOs;

/// <param name="MemberCount">Members whose participation has not ended (active and suspended; removed members are not counted).</param>
/// <param name="Version">The Workspace aggregate version, sent back as <c>expectedVersion</c> by profile, settings, archive and delete writes.</param>
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
    DateTime CreatedAt,
    string? Settings,
    long Version
);

/// <param name="Status">The membership status enum name (<c>Active</c> or <c>Suspended</c>). Removed members are not listed: removal ends the participation (docs/product/workspaces.md §13, §20).</param>
public record WorkspaceMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt,
    string Status
);

/// <param name="Status">The invitation status enum name (<c>Pending</c>, <c>Accepted</c>, <c>Revoked</c>, <c>Expired</c> or <c>Declined</c>), as stored. A <c>Pending</c> invitation whose <c>ExpiresAt</c> has passed can no longer be accepted.</param>
public record WorkspaceInvitationDto(
    Guid Id,
    string Email,
    string Role,
    DateTime ExpiresAt,
    bool IsAccepted,
    DateTime CreatedAt,
    string Status
);
