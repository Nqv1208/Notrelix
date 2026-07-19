namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record CompleteBoardItemRequest(DateTimeOffset? CompletedAt = null);
