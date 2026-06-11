using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Teams;



public class Team : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public TeamStatus Status { get; private set; }

    private readonly List<TeamMember> _members = new();
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    private Team() : base() { }

    public static Team Create(Guid workspaceId, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var team = new Team
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Status = TeamStatus.Active
        };

        team.SetAuditOnCreate(createdBy, createdAt);
        team.AddDomainEvent(new TeamCreatedEvent(team.Id, workspaceId, team.Name, createdBy, createdAt));

        return team;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new TeamRenamedEvent(WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived) return;

        Status = TeamStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        AddDomainEvent(new TeamArchivedEvent(WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void AddMember(Guid userId, TeamMemberRole role, Guid addedBy, DateTimeOffset addedAt, Guid? workspaceMemberId = null)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException("Cannot add a member to an archived team.");
        if (_members.Any(m => m.UserId == userId))
            throw new BusinessRuleException("User is already a member of this team.");

        var member = TeamMember.Create(Id, userId, role, workspaceMemberId);
        _members.Add(member);
        
        SetAuditOnUpdate(addedBy, addedAt);
        AddDomainEvent(new TeamMemberAddedEvent(WorkspaceId, Id, userId, role, addedBy, addedAt));
    }

    public void RemoveMember(Guid userId, Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        if (Status == TeamStatus.Archived)
            throw new BusinessRuleException("Cannot remove a member from an archived team.");
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return;

        member.Remove(removedBy, removedAt);
        SetAuditOnUpdate(removedBy, removedAt);
        AddDomainEvent(new TeamMemberRemovedEvent(WorkspaceId, Id, userId, removedBy, removedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = TeamStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new TeamSoftDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = TeamStatus.Active;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new TeamRestoredEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
