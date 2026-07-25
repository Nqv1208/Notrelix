namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

public sealed record CreateSpaceRequest(string Name, string Visibility, string? Description);
