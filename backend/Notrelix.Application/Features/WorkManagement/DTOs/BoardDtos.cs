namespace Notrelix.Application.Features.WorkManagement.DTOs;

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
    List<BoardFieldDto> Columns,
    List<BoardGroupDto> Lists,
    List<BoardMemberDto> Members
);

public record BoardFieldDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string FieldType,
    string Settings,
    string? DefaultValue,
    string Position,
    bool IsSystem,
    bool IsDeleted
);

public record BoardMemberDto(
    Guid UserId,
    string Name,
    string? Avatar,
    string Role,
    DateTime JoinedAt
);

public record BoardGroupDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsArchived,
    List<BoardItemSummaryDto> BoardItems
);

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

public record ChecklistDto(
    Guid Id,
    string Title,
    string Position,
    List<ChecklistItemDto> Items
);

public record ChecklistItemDto(
    Guid Id,
    string Title,
    string Status,
    DateTime? DueAt,
    Guid? AssigneeUserId,
    string Position
);

// ────────────────────────────────────────────────────────
// Monday-like Work OS DTOs (Merged from DTOs.cs)
// ────────────────────────────────────────────────────────

public record BoardFieldSchemaDto(
    Guid Id,
    string Name,
    string Type,
    string Settings,
    string? DefaultValue,
    string Position,
    bool IsSystem);

public record BoardGroupSchemaDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsCollapsed);

public record BoardItemSlimDto(
    Guid Id,
    Guid GroupId,
    string Name,
    string Position,
    List<Guid> MemberIds,
    List<Guid> LabelIds);

public record BoardSchemaDto(
    Guid Id,
    string Title,
    string? Description,
    List<BoardFieldSchemaDto> Fields,
    List<BoardGroupSchemaDto> Groups);

public record BoardViewDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string Type,
    string Config,
    bool IsDefault);
