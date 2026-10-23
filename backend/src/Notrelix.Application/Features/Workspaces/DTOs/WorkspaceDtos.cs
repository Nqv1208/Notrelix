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
/// <param name="JobTitle">The member's Workspace job title (Workspaces-owned member profile); null when none is set.</param>
/// <param name="Version">The WorkspaceMember aggregate version, sent back as <c>expectedVersion</c> by the job-title writes.</param>
/// <param name="Phone">The person's account phone in E.164 form. Present only when the person turned on <c>phoneVisibleToWorkspaceMembers</c> and the reader is a non-guest member of this Workspace; never present for guests, share links or public views.</param>
public record WorkspaceMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt,
    string Status,
    string? JobTitle = null,
    long Version = 0,
    string? Phone = null
);

/// <summary>The Workspace member profile fields a job-title write returns; <see cref="Version"/> is the next <c>expectedVersion</c>.</summary>
public record WorkspaceMemberProfileDto(Guid UserId, string? JobTitle, long Version);

/// <param name="ResourceKind">Set for a resource invitation (WSP-026): <c>work-management.board</c> or <c>documents.page</c>; null for a Workspace invitation.</param>
/// <param name="ResourceId">The one resource a resource invitation shares.</param>
/// <param name="ResourceLevel">The explicit entry level created on acceptance: <c>Viewer</c>, <c>Commenter</c> or <c>Editor</c> (capped, GOV-030).</param>
/// <param name="InitialEntries">The Team/Space entries applied atomically on acceptance (Workspace invitation only).</param>
/// <param name="Status">The invitation status enum name (<c>Pending</c>, <c>Accepted</c>, <c>Revoked</c>, <c>Expired</c> or <c>Declined</c>), as stored. A <c>Pending</c> invitation whose <c>ExpiresAt</c> has passed can no longer be accepted.</param>
public record WorkspaceInvitationDto(
    Guid Id,
    string Email,
    string Role,
    DateTime ExpiresAt,
    bool IsAccepted,
    DateTime CreatedAt,
    string Status,
    string? ResourceKind = null,
    Guid? ResourceId = null,
    string? ResourceLevel = null,
    IReadOnlyList<WorkspaceInvitationEntryDto>? InitialEntries = null
);

/// <param name="Kind"><c>Team</c> or <c>Space</c>.</param>
public record WorkspaceInvitationEntryDto(string Kind, Guid TargetId, string Role);
