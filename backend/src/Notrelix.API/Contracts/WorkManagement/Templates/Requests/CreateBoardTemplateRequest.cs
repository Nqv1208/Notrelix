namespace Notrelix.API.Contracts.WorkManagement.Templates.Requests;

public record CreateBoardTemplateRequest(string Name, string? Description = null, bool IncludeItems = false);

/// <param name="SpaceId">Space to create the board in; omitted creates it at the workspace root.</param>
public record CreateBoardFromTemplateRequest(string WorkspaceId, string Name, Guid? SpaceId = null);
