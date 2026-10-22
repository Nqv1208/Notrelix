namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

/// <param name="Role"><c>Member</c> or <c>Guest</c> (WSP-026).</param>
/// <param name="InitialEntries">Optional Team/Space entries applied atomically when the invitation is accepted; naming them needs a Workspace Owner/Admin.</param>
public record InviteMemberRequest(string Email, string Role, IReadOnlyList<InvitationInitialEntryRequest>? InitialEntries = null);

/// <param name="Kind"><c>Team</c> or <c>Space</c>.</param>
/// <param name="Role">Team: <c>Member</c> or <c>Lead</c>. Space: <c>Viewer</c>, <c>Member</c> or <c>Admin</c>. A Guest invitation cannot name a Team lead or Space admin.</param>
public record InvitationInitialEntryRequest(string Kind, Guid TargetId, string Role);
