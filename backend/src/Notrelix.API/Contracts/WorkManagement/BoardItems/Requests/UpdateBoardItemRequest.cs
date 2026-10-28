namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// PATCH /board-items/{itemId} (WM-V3-API-001): Title/DueDate/StartDate/DescriptionMd; an
/// omitted field is unchanged and the item identity comes from the route only. An empty
/// DescriptionMd clears the description; ClearDueDate / ClearStartDate remove a date (a null date
/// alone means "unchanged"), and cannot be sent together with that date.
/// </summary>
public record UpdateBoardItemRequest(
    long ExpectedVersion,
    string? Title = null,
    DateTime? DueDate = null,
    DateTime? StartDate = null,
    string? DescriptionMd = null,
    bool ClearDueDate = false,
    bool ClearStartDate = false);
