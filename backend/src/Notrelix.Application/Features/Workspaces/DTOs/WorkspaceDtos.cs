namespace Notrelix.Application.Features.Workspaces.DTOs;

/// <param name="MemberCount">Members whose participation has not ended (active and suspended; removed members are not counted).</param>
/// <param name="Version">The Workspace aggregate version, sent back as <c>expectedVersion</c> by profile, settings, archive and delete writes.</param>
/// <param name="Size">The company-size band (<c>1-10</c>, <c>11-50</c>, <c>51-200</c>, <c>201-1000</c> or <c>1000+</c>); null until an Owner or Admin sets it.</param>
/// <param name="Language">The workspace language, <c>vi</c> or <c>en</c>; null until set.</param>
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
    long Version,
    string? Size = null,
    string? Language = null
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

/// <summary>
/// One Workspace the caller takes part in, as the account's "your workspaces" page needs it.
/// <see cref="Role"/> is the caller's own role. <see cref="MemberCount"/> is the number of active
/// members and is null for a Guest (a Guest may not see the member list, GOV-009).
/// <see cref="DepartmentCount"/> is the number of active department Spaces and is null unless the
/// caller is an Owner or Admin. <see cref="IsDefault"/> marks the Workspace that opens with the app.
/// <see cref="CanLeave"/> is false only when the caller is the last active Owner.
/// <see cref="IsArchived"/> marks an archived Workspace, which is listed only to its Owners and Admins; it is
/// read-only for every member and is never the default.
/// </summary>
public record AccountWorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    string Role,
    int? MemberCount,
    int? DepartmentCount,
    bool IsDefault,
    bool CanLeave,
    bool IsArchived = false
);
