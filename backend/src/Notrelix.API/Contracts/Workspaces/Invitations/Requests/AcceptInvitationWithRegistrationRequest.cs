namespace Notrelix.API.Contracts.Workspaces.Invitations.Requests;

/// <summary>Accept an invitation as a person with no account: the invited address is fixed by the invitation.</summary>
public sealed record AcceptInvitationWithRegistrationRequest(string Token, string Name, string Password);
