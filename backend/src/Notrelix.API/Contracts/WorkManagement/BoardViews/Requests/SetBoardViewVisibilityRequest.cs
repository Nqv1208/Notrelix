namespace Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;

public record SetBoardViewVisibilityRequest(string Visibility, long ExpectedVersion);
