namespace Notrelix.API.Contracts.Workspaces.Invitations.Requests;

/// <param name="Email">The invitee's email; the invitation can be accepted only by an identity with this verified email.</param>
/// <param name="Level"><c>Viewer</c>, <c>Commenter</c> or <c>Editor</c>: a Guest resource entry is capped at Editor (GOV-030).</param>
public sealed record InviteGuestToResourceRequest(string Email, string Level);
