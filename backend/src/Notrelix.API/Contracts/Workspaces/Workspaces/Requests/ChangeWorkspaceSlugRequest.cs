namespace Notrelix.API.Contracts.Workspaces.Workspaces.Requests;

public sealed record ChangeWorkspaceSlugRequest(
    string Slug,
    long ExpectedVersion);
