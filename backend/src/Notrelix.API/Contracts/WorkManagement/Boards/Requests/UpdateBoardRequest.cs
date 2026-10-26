namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <summary>
/// PATCH /boards/{boardId} (WM-V3-API-001): an omitted field is unchanged; the board
/// identity comes from the route only, so no body field is silently overwritten.
/// </summary>
public record UpdateBoardRequest(long ExpectedVersion, string? Title = null, string? Description = null, string? Background = null, BoardVisibility? Visibility = null, string? ItemKeyPrefix = null);
