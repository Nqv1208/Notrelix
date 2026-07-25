using Notrelix.Domain.Workspaces.Teams.Events;
using Notrelix.Domain.Workspaces.Rules;
namespace Notrelix.Domain.Workspaces.Teams;

public class Team : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public TeamStatus Status { get; private set; }

    private readonly List<TeamMember> _members = new();
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    private Team() : base() { }

    public static Team Create(Guid accountId, Guid workspaceId, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotEmpty(createdBy);

        var team = new Team
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Status = TeamStatus.Active
        };

        team.SetAuditOnCreate(createdBy, createdAt);
        team.RaiseDomainEvent(new TeamCreatedDomainEvent(team.Id, accountId, workspaceId, team.Name, createdBy, createdAt));

        return team;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotEmpty(updatedBy);

        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_CannotRenameArchived, "Cannot rename an archived team.");

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? newDescription, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_CannotUpdateDescriptionArchived, "Cannot update description of an archived team.");

        var normalized = string.IsNullOrWhiteSpace(newDescription)
            ? null
            : newDescription.Trim();

        if (normalized is not null)
            Guard.MaxLength(normalized, 1024);

        if (Description == normalized) return;

        var oldDescription = Description;
        Description = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamDescriptionUpdatedDomainEvent(
            AccountId, WorkspaceId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == TeamStatus.Archived) return;

        Status = TeamStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);

        if (Status == TeamStatus.Active) return;

        if (Status != TeamStatus.Archived)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Team_CannotUnarchiveNonArchived,
                "Only an archived team can be unarchived.");

        Status = TeamStatus.Active;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamUnarchivedDomainEvent(
            AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    public void AddMember(Guid userId, TeamMemberRole role, Guid addedBy, DateTimeOffset addedAt, Guid? workspaceMemberId = null)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_CannotAddMemberArchived, "Cannot add a member to an archived team.");
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing != null)
        {
            if (existing.Status == TeamMemberStatus.Active) return;

            existing.Reactivate(role, workspaceMemberId, addedBy, addedAt);
        }
        else
        {
            var member = TeamMember.Create(AccountId, WorkspaceId, Id, userId, role, addedBy, addedAt, workspaceMemberId);
            _members.Add(member);
        }

        SetAuditOnUpdate(addedBy, addedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamMemberAddedDomainEvent(AccountId, WorkspaceId, Id, userId, role, addedBy, addedAt));
    }

    public void RemoveMember(Guid userId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_CannotRemoveMemberArchived, "Cannot remove a member from an archived team.");
        Guard.NotEmpty(userId);
        Guard.NotEmpty(removedBy);

        var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == TeamMemberStatus.Active);
        if (member == null) return;

        if (member.Role == TeamMemberRole.Lead)
        {
            var activeLeadCount = _members.Count(m => m.Status == TeamMemberStatus.Active && m.Role == TeamMemberRole.Lead);
            TeamLeadRules.EnsureCanRemoveLead(activeLeadCount);
        }

        member.Remove(removedBy, removedAt);
        SetAuditOnUpdate(removedBy, removedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, userId, removedBy, removedAt));
    }

    public void ChangeMemberRole(Guid userId, TeamMemberRole newRole, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(userId);
        Guard.NotEmpty(updatedBy);

        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_CannotChangeMemberRoleArchived, "Cannot change member role in an archived team.");

        var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == TeamMemberStatus.Active);
        if (member == null)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Team_UserNotActiveMember, "User is not an active member of this team.");

        var activeLeadCount = _members.Count(m => m.Status == TeamMemberStatus.Active && m.Role == TeamMemberRole.Lead);
        TeamLeadRules.EnsureCanDowngradeLead(member.Role, newRole, activeLeadCount);

        if (member.Role == newRole) return;

        var oldRole = member.Role;
        member.ChangeRole(newRole, updatedBy, updatedAt);

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, userId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        Status = TeamStatus.SoftDeleted;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        Status = TeamStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new TeamRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
