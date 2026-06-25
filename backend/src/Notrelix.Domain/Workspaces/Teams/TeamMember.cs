namespace Notrelix.Domain.Workspaces.Teams;

public class TeamMember : AuditableEntity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamMemberRole Role { get; private set; }
    public TeamMemberStatus Status { get; private set; }
    public Guid? WorkspaceMemberId { get; private set; }

    private TeamMember() : base() { }

    public static TeamMember Create(
        Guid workspaceId,
        Guid teamId,
        Guid userId,
        TeamMemberRole role,
        Guid addedBy,
        DateTimeOffset createdAt,
        Guid? workspaceMemberId = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(teamId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var member = new TeamMember
        {
            WorkspaceId = workspaceId,
            TeamId = teamId,
            UserId = userId,
            Role = role,
            Status = TeamMemberStatus.Active,
            WorkspaceMemberId = workspaceMemberId
        };

        member.SetAuditOnCreate(addedBy, createdAt);
        return member;
    }

    public void Reactivate(TeamMemberRole role, Guid? workspaceMemberId, Guid activatedBy, DateTimeOffset activatedAt)
    {
        Status = TeamMemberStatus.Active;
        Role = role;
        WorkspaceMemberId = workspaceMemberId;
        SetAuditOnUpdate(activatedBy, activatedAt);
    }

    public void Remove(Guid removedBy, DateTimeOffset removedAt)
    {
        Guard.NotEmpty(removedBy);

        if (Status == TeamMemberStatus.Removed) return;

        Status = TeamMemberStatus.Removed;
        SetAuditOnUpdate(removedBy, removedAt);
    }
}
