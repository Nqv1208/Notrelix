namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record MoveBoardItemRequest(Guid GroupId, double Position);
