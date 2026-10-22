namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>One initial Team or Space entry applied atomically when the invitation is accepted (WSP-026).</summary>
/// <param name="Kind"><c>Team</c> or <c>Space</c>.</param>
/// <param name="Role">Team: <c>Member</c> or <c>Lead</c>. Space: <c>Viewer</c>, <c>Member</c> or <c>Admin</c>.</param>
public record InvitationInitialEntryInput(string Kind, Guid TargetId, string Role);
