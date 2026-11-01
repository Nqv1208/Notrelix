namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary><c>daily</c>, <c>weekdays</c>, <c>weekly</c>, <c>biweekly</c> or <c>monthly</c>; null stops the repeat.</summary>
public record SetBoardItemRepeatRequest(string? Rule, long ExpectedVersion);
