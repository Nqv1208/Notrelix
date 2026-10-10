namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

public record UpdateBoardGroupRequest(long ExpectedVersion, string? Title = null, string? Color = null);
