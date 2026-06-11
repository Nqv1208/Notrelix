using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Teams;

public class TeamMember : Entity
{
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamMemberRole Role { get; private set; }

    private TeamMember() : base() { }

    public static TeamMember Create(Guid teamId, Guid userId, TeamMemberRole role)
    {
        Guard.NotEmpty(teamId);
        Guard.NotEmpty(userId);

        return new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            Role = role
        };
    }
}
