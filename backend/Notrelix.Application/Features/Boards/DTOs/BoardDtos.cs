namespace Notrelix.Application.Features.Boards.DTOs;

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
    Guid WorkspaceId,
    string Title,
    string? Description,
    string Background,
    string Visibility,
    List<BoardColumnDto> Columns,
    List<ListDto> Lists,
    List<BoardMemberDto> Members
);

public record BoardColumnDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string FieldType,
    string Settings,
    double Position,
    bool IsHidden,
    bool IsSystemField
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
    string Color,
    double Position,
    bool IsArchived,
    List<CardSummaryDto> Cards
);

public record CardSummaryDto(
    Guid Id,
    string Title,
    Guid? LinkedPageId,
    string? Priority,
    string Status,
    DateTime? DueDate,
    DateTime? StartDate,
    DateTime? CompletedAt,
    string? Cover,
    int MemberCount,
    List<CardMemberDto> Members,
    List<CardLabelDto> Labels,
    int ChecklistProgress, // e.g. 3/5
    int ChecklistTotal,
    int CommentCount,
    int AttachmentCount,
    double Position,
    string FieldValues,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CardDto(
    Guid Id,
    Guid BoardId,
    Guid WorkspaceId,
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
    int CommentCount,
    int AttachmentCount,
    string FieldValues,
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
