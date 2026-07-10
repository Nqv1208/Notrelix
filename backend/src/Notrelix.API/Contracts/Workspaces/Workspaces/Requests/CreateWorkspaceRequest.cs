namespace Notrelix.API.Contracts.Workspaces.Workspaces.Requests;

public record CreateWorkspaceRequest(string Name, string? Description, bool IsPersonal);
