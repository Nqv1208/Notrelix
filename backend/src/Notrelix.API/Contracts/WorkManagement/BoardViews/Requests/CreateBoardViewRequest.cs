namespace Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;

public record CreateBoardViewRequest(string Name, string ViewMode, double Position, string? Config = null, string? Visibility = null);
