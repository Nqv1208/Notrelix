namespace Notrelix.Domain.Workspaces.Teams;

public class Team : AggregateRoot, IWorkspaceScoped
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
        team.AddDomainEvent(new TeamCreatedDomainEvent(team.Id, accountId, workspaceId, team.Name, createdBy, createdAt));

        return team;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotEmpty(updatedBy);

        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException("Cannot rename an archived team.");

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new TeamRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == TeamStatus.Archived) return;

        Status = TeamStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new TeamArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void AddMember(Guid userId, TeamMemberRole role, Guid addedBy, DateTimeOffset addedAt, Guid? workspaceMemberId = null)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException("Cannot add a member to an archived team.");
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing != null)
        {
            if (existing.Status == TeamMemberStatus.Active)
                throw new BusinessRuleException("User is already a member of this team.");

            existing.Reactivate(role, workspaceMemberId, addedBy, addedAt);
        }
        else
        {
            var member = TeamMember.Create(AccountId, WorkspaceId, Id, userId, role, addedBy, addedAt, workspaceMemberId);
            _members.Add(member);
        }

        SetAuditOnUpdate(addedBy, addedAt);
        IncrementVersion();
        AddDomainEvent(new TeamMemberAddedDomainEvent(AccountId, WorkspaceId, Id, userId, role, addedBy, addedAt));
    }

    public void RemoveMember(Guid userId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException("Cannot remove a member from an archived team.");
        Guard.NotEmpty(userId);
        Guard.NotEmpty(removedBy);

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return;

        member.Remove(removedBy, removedAt);
        SetAuditOnUpdate(removedBy, removedAt);
        IncrementVersion();
        AddDomainEvent(new TeamMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, userId, removedBy, removedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        Status = TeamStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new TeamSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        Status = TeamStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new TeamRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
