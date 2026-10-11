namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="SpaceId">Space to create the board in (Editor level on that Space required); omitted creates it at the workspace root.</param>
public record CreateBoardInWorkspaceRequest(string Title, string? Description = null, string? Background = null, string? Visibility = null, Guid? SpaceId = null);
