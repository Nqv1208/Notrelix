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
    string? ItemKeyPrefix = null
);

public record BoardMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);
