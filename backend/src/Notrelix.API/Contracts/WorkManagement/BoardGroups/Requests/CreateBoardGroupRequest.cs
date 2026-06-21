namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

public record CreateBoardGroupRequest(string Title, double? Position = null, string? Color = null);
