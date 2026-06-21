namespace Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;

public record SaveBoardViewRequest(string ViewMode, string? Filters = null, string? Config = null);
