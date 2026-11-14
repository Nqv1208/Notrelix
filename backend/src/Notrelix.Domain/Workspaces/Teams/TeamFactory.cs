namespace Notrelix.Domain.Workspaces.Teams;

public static class TeamFactory
{
    public static (Team Team, TeamMember Lead) CreateWithLead(
        Guid accountId,
        Guid workspaceId,
        string name,
        Guid creatorUserId,
        DateTimeOffset createdAt,
        Guid? workspaceMemberId = null,
        IReadOnlyCollection<string>? otherTeamNames = null)
    {
        var team = Team.Create(accountId, workspaceId, name, creatorUserId, createdAt, otherTeamNames);
        team.AddMember(creatorUserId, TeamMemberRole.Lead, creatorUserId, createdAt, workspaceMemberId);

        var lead = team.Members.First(m => m.UserId == creatorUserId);
        return (team, lead);
    }
}
