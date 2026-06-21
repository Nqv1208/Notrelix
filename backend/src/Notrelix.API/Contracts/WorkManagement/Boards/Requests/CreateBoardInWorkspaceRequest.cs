namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

public record CreateBoardInWorkspaceRequest(string Title, string? Description = null, string? Background = null, string? Visibility = null);
