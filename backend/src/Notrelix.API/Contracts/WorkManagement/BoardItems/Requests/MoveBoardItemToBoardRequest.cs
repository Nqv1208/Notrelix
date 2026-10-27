namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>The board of the workspace the item moves to.</summary>
public record MoveBoardItemToBoardRequest(Guid BoardId);
