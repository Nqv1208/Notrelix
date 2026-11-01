namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <param name="Operation">set-status, set-due-date, clear-due-date, assign, unassign-all, archive, set-priority or delete.</param>
/// <param name="ItemIds">The items of the board to change (1 to 200).</param>
/// <param name="ExpectedVersions">The version the caller last read of every named item, by item id; one stale item refuses the request.</param>
/// <param name="Status">The status option id (set-status).</param>
/// <param name="DueDate">The due date, UTC (set-due-date).</param>
/// <param name="AssigneeUserId">The member who becomes the only assignee (assign).</param>
/// <param name="Priority">The option id of the board's Priority field (set-priority).</param>
public record BulkUpdateBoardItemsRequest(
    string Operation,
    List<Guid> ItemIds,
    Dictionary<Guid, long> ExpectedVersions,
    string? Status,
    DateTime? DueDate,
    Guid? AssigneeUserId,
    string? Priority = null);
