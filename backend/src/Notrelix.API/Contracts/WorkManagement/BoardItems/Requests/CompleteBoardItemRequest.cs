namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record CompleteBoardItemRequest(long ExpectedVersion, DateTimeOffset? CompletedAt = null);
