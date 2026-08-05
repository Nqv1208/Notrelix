namespace Notrelix.API.Contracts.WorkManagement.Templates.Requests;

public record CreateBoardTemplateRequest(string Name, string? Description = null);

public record CreateBoardFromTemplateRequest(string WorkspaceId, string Name);
