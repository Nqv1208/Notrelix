namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardItemSummaryDto(
    Guid Id,
    string Name,
    int MemberCount,
    List<BoardItemMemberDto> Members,
    List<BoardItemLabelDto> Labels,
    int ChecklistProgress, // e.g. 3/5
    int ChecklistTotal,
    int CommentCount,
    int AttachmentCount,
    string Position,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record BoardItemDto(
    Guid Id,
    Guid BoardId,
    Guid WorkspaceId,
    Guid GroupId,
    string Name,
    List<BoardItemMemberDto> Members,
    List<BoardItemLabelDto> Labels,
    List<ChecklistDto> Checklists,
    int CommentCount,
    int AttachmentCount,
    string Position,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record BoardItemMemberDto(Guid UserId, string Name, string? Avatar, DateTimeOffset AssignedAt);
public record BoardItemLabelDto(Guid LabelId, string? Name, string Color);

public record BoardItemSlimDto(
    Guid Id,
    Guid GroupId,
    string Name,
    string Position,
    List<Guid> MemberIds,
    List<Guid> LabelIds);
