namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record CreateBoardItemRequest(Guid GroupId, string Title, double Position);
