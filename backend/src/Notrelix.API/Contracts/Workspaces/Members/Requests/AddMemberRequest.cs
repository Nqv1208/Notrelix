namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

public sealed record AddMemberRequest(Guid UserId, WorkspaceRole Role);
