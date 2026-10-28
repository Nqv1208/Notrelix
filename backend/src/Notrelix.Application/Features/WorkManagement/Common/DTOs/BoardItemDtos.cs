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
    BoardItemChildProgressDto? ChildProgress = null,
    // Derived: live, not-completed Items this Item is blocked by (Dependency links where it is the source).
    int OpenBlockerCount = 0
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
    bool IsArchived = false,
    // Who completed or archived the Item, with the display name resolved at read time.
    Guid? CompletedBy = null,
    string? CompletedByName = null,
    DateTimeOffset? ArchivedAt = null,
    Guid? ArchivedBy = null,
    string? ArchivedByName = null,
    // The item's Markdown description; null when it has none.
    string? DescriptionMd = null
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

/// <summary>A sub-item as its parent lists it. <see cref="Done"/> is whether it is completed.</summary>
public record ChildItemDto(
    Guid Id,
    string? Key,
    string Title,
    bool Done,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    string? AssigneeName,
    string Position,
    long Version);

public record ChildItemListDto(IReadOnlyList<ChildItemDto> Items);
