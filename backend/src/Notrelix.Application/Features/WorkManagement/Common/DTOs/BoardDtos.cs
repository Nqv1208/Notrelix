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
    long Version
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
    long Version
);

public record BoardMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);
