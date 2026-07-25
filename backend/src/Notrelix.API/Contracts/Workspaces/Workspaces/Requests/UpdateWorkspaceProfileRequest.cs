namespace Notrelix.API.Contracts.Workspaces.Workspaces.Requests;

public sealed record UpdateWorkspaceProfileRequest(
    string? Name,
    string? Description,
    long ExpectedVersion);
