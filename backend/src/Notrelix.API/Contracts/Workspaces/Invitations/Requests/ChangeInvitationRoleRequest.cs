namespace Notrelix.API.Contracts.Workspaces.Invitations.Requests;

/// <param name="ExpectedVersion">The invitation's <c>version</c> as last read; a version that moved answers 412 and nothing changes.</param>
public sealed record ChangeInvitationRoleRequest(WorkspaceRole NewRole, long ExpectedVersion);
