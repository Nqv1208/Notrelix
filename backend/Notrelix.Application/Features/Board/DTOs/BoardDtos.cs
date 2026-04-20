namespace Notrelix.Application.Features.Board.DTOs;

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
    DateTime CreatedAt
);

public record FullBoardDto(
    Guid Id,
    string Title,
    string? Description,
    string Background,
    string Visibility,
    List<ListDto> Lists,
    List<BoardMemberDto> Members
);

public record BoardMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);

public record ListDto(
    Guid Id,
    string Title,
    double Position,
    bool IsArchived,
    List<CardSummaryDto> Cards
);

public record CardSummaryDto(
    Guid Id,
    string Title,
    string? Priority,
    string Status,
    DateTime? DueDate,
    string? Cover,
    int MemberCount,
    int ChecklistProgress, // e.g. 3/5
    int ChecklistTotal,
    int CommentCount,
    int AttachmentCount,
    double Position
);

public record CardDto(
    Guid Id,
    Guid ListId,
    string Title,
    string? DescriptionMd,
    Guid? LinkedPageId,
    string? Priority,
    string Status,
    DateTime? DueDate,
    DateTime? StartDate,
    DateTime? CompletedAt,
    string? Cover,
    double Position,
    List<CardMemberDto> Members,
    List<CardLabelDto> Labels,
    List<ChecklistDto> Checklists,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CardMemberDto(Guid UserId, string Name, string? Avatar, DateTime AssignedAt);
public record CardLabelDto(Guid LabelId, string? Name, string Color);

public record ChecklistDto(
    Guid Id,
    string Title,
    double Position,
    List<ChecklistItemDto> Items
);

public record ChecklistItemDto(
    Guid Id,
    string Title,
    bool IsChecked,
    DateTime? DueDate,
    Guid? AssigneeId,
    double Position
);
