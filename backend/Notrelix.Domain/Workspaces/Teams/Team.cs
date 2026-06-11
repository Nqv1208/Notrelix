using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

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

public class Team : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public TeamStatus Status { get; private set; }

    private readonly List<TeamMember> _members = new();
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    private Team() : base() { }

    public static Team Create(Guid workspaceId, string name, Guid createdBy)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var team = new Team
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Status = TeamStatus.Active
        };

        team.SetAuditOnCreate(createdBy);
        team.AddDomainEvent(new TeamCreatedEvent(team.Id, workspaceId, team.Name, createdBy));

        return team;
    }

    public void AddMember(Guid userId, TeamMemberRole role, Guid addedBy)
    {
        EnsureNotDeleted();
        if (_members.Any(m => m.UserId == userId))
            throw new BusinessRuleException("User is already a member of this team.");

        var member = TeamMember.Create(Id, userId, role);
        _members.Add(member);
        
        SetAuditOnUpdate(addedBy);
        AddDomainEvent(new TeamMemberAddedEvent(Id, userId, role, addedBy));
    }

    public void RemoveMember(Guid userId, Guid removedBy)
    {
        EnsureNotDeleted();
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return;

        _members.Remove(member);
        SetAuditOnUpdate(removedBy);
    }
}
