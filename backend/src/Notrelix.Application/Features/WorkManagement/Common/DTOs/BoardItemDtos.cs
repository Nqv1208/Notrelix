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
    DateTime? UpdatedAt,
    long Version,
    // Board-scoped key ("WEB-12", or "12" on a board without prefix); null only for legacy data.
    string? ItemKey = null,
    Guid? ParentItemId = null,
    // Option id held by the board's Status field, and by its priority field.
    Guid? StatusOptionId = null,
    Guid? PriorityOptionId = null,
    DateTimeOffset? DueAt = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    // Derived from non-archived child Items; null when the Item has none.
    BoardItemChildProgressDto? ChildProgress = null
);

/// <summary>
/// Derived, read-only progress of an Item with child Items: <see cref="Done"/>
/// children whose status is in the done category over <see cref="Total"/>
/// non-archived children.
/// </summary>
public record BoardItemChildProgressDto(int Done, int Total);

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
    DateTime? UpdatedAt,
    long Version,
    string? ItemKey = null,
    Guid? ParentItemId = null,
    Guid? StatusOptionId = null,
    Guid? PriorityOptionId = null,
    DateTimeOffset? DueAt = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    BoardItemChildProgressDto? ChildProgress = null,
    bool IsArchived = false
);

public record BoardItemMemberDto(Guid UserId, string Name, string? Avatar, DateTimeOffset AssignedAt);
public record BoardItemLabelDto(Guid LabelId, string? Name, string Color);

public record BoardItemSlimDto(
    Guid Id,
    Guid GroupId,
    string Name,
    string Position,
    List<Guid> MemberIds,
    List<Guid> LabelIds,
    long Version);
