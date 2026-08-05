namespace Notrelix.Domain.Workspaces.Teams;

public class TeamMember : AuditableEntity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamMemberRole Role { get; private set; }
    public TeamMemberStatus Status { get; private set; }
    public Guid? WorkspaceMemberId { get; private set; }

    private TeamMember() : base() { }

    public static TeamMember Create(
        Guid accountId,
        Guid workspaceId,
        Guid teamId,
        Guid userId,
        TeamMemberRole role,
        Guid addedBy,
        DateTimeOffset createdAt,
        Guid? workspaceMemberId = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(teamId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var member = new TeamMember
        {
            AccountId = accountId,
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
        Guard.NotEmpty(activatedBy);

        var audit = PrepareAuditUpdate(activatedBy, activatedAt);

        if (Status == TeamMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_TeamMember_AlreadyActive, "Team member is already active.");

        Status = TeamMemberStatus.Active;
        Role = role;
        WorkspaceMemberId = workspaceMemberId;
        ApplyAuditUpdate(audit);
    }

    public void ChangeRole(TeamMemberRole newRole, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status != TeamMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_TeamMember_CannotChangeRoleOfInactive, "Cannot change the role of an inactive team member.");
        if (Role == newRole) return;
        Role = newRole;
        ApplyAuditUpdate(audit);
    }

    public void Remove(Guid removedBy, DateTimeOffset removedAt)
    {
        Guard.NotEmpty(removedBy);

        var audit = PrepareAuditUpdate(removedBy, removedAt);

        if (Status == TeamMemberStatus.Removed) return;

        Status = TeamMemberStatus.Removed;
        ApplyAuditUpdate(audit);
    }
}
