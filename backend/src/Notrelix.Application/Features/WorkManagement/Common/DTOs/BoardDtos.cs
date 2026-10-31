namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardDto(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string? Description,
    string Background,
    string Visibility,
    bool IsArchived,
    int MemberCount,
    int ListCount,
    DateTime CreatedAt,
    long Version,
    // The Workspaces Space the board is placed in; null at the Workspace root
    // and also when the caller cannot see that Space (never disclosed, GOV-029).
    Guid? SpaceId = null
);

public record FullBoardDto(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string? Description,
    string Background,
    string Visibility,
    List<BoardFieldDto> Columns,
    List<BoardGroupDto> Lists,
    List<BoardMemberDto> Members,
    long Version,
    // The Workspaces Space the board is placed in; null at the Workspace root and
    // when the caller cannot see that Space (never disclosed).
    Guid? SpaceId = null,
    // Prefix of the board's Item keys ("WEB" in "WEB-12"); null when keys are plain numbers.
    string? ItemKeyPrefix = null,
    // The board is archived: it reads, every write is refused until a Manager unarchives it.
    bool IsArchived = false,
    // The board's Viewers (read-only roles): listed apart, never among the members.
    List<BoardMemberDto>? Viewers = null
);

/// <summary>
/// A board that left the working set: archived (kept, read-only) or deleted (restorable until
/// <paramref name="RestorableUntil"/>, then purged).
/// </summary>
/// <param name="RetiredAt">When it was archived or deleted.</param>
/// <param name="RestorableUntil">Deleted boards only: the end of the 30-day restore window.</param>
public record RetiredBoardDto(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string Background,
    string Visibility,
    long Version,
    Guid? SpaceId,
    string? ItemKeyPrefix,
    DateTime RetiredAt,
    DateTime? RestorableUntil);

/// <summary>What archiving or deleting a board touches, for the confirmation that names it.</summary>
/// <param name="ItemCount">Active items (not archived, not deleted), sub-items included.</param>
/// <param name="OpenItemCount">Of <paramref name="ItemCount"/>, those not completed.</param>
public record BoardLifecycleImpactDto(
    Guid BoardId,
    bool IsArchived,
    int ItemCount,
    int OpenItemCount,
    int MemberCount,
    int GroupCount,
    int ViewCount);

public record BoardMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);
