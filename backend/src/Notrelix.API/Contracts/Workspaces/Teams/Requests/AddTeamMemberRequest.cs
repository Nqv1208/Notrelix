namespace Notrelix.API.Contracts.Workspaces.Teams.Requests;

public sealed record AddTeamMemberRequest(Guid UserId, string Role);
