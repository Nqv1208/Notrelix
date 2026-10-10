namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// PATCH /board-items/{itemId} (WM-V3-API-001): exactly Title/DueDate/StartDate; an
/// omitted field is unchanged and the item identity comes from the route only.
/// </summary>
public record UpdateBoardItemRequest(long ExpectedVersion, string? Title = null, DateTime? DueDate = null, DateTime? StartDate = null);
