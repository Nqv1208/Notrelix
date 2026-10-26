namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <param name="Operation">set-status, set-due-date, clear-due-date, assign, unassign-all or archive.</param>
/// <param name="ItemIds">The items of the board to change (1 to 200).</param>
/// <param name="Status">The status option id (set-status).</param>
/// <param name="DueDate">The due date, UTC (set-due-date).</param>
/// <param name="AssigneeUserId">The member who becomes the only assignee (assign).</param>
public record BulkUpdateBoardItemsRequest(
    string Operation,
    List<Guid> ItemIds,
    string? Status,
    DateTime? DueDate,
    Guid? AssigneeUserId);
