using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Workspaces;

public class Workspace : SoftDeletableEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkspaceStatus Status { get; private set; }
    public WorkspaceSettings Settings { get; private set; } = null!;
    public bool IsPersonal { get; private set; }

    private readonly List<Notrelix.Domain.Workspaces.Members.WorkspaceMember> _workspaceMembers = new();
    public IReadOnlyCollection<Notrelix.Domain.Workspaces.Members.WorkspaceMember> WorkspaceMembers => _workspaceMembers.AsReadOnly();

    private Workspace() : base() { }

    public static Workspace Create(Guid ownerId, string name, string slug, bool isPersonal = false)
    {
        Guard.NotEmpty(ownerId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(slug);

        var workspace = new Workspace
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Status = WorkspaceStatus.Active,
            Settings = WorkspaceSettings.Create(),
            IsPersonal = isPersonal
        };

        workspace.SetAuditOnCreate(ownerId);
        
        // The first member is the owner
        var owner = Notrelix.Domain.Workspaces.Members.WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner, ownerId);
        workspace._workspaceMembers.Add(owner);

        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.Slug, ownerId));

        return workspace;
    }

    public void Rename(string newName, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);

        var oldName = Name;
        if (Name == newName.Trim()) return;

        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new WorkspaceRenamedEvent(Id, oldName, Name, updatedBy));
    }

    public void Archive(Guid archivedBy)
    {
        EnsureNotDeleted();
        if (Status == WorkspaceStatus.Archived) return;

        Status = WorkspaceStatus.Archived;
        SetAuditOnUpdate(archivedBy);
        AddDomainEvent(new WorkspaceArchivedEvent(Id, archivedBy));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = WorkspaceStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new WorkspaceSoftDeletedEvent(Id, deletedBy));
    }

    public void AddMember(Guid userId, WorkspaceRole role, Guid addedBy)
    {
        EnsureNotDeleted();
        if (_workspaceMembers.Any(m => m.UserId == userId))
            throw new BusinessRuleException("User is already a member of this workspace.");

        var member = Notrelix.Domain.Workspaces.Members.WorkspaceMember.Create(Id, userId, role, addedBy);
        _workspaceMembers.Add(member);
        AddDomainEvent(new WorkspaceMemberAddedEvent(Id, member.Id, userId, role, addedBy));
    }

    public void RemoveMember(Guid userId, Guid removedBy)
    {
        EnsureNotDeleted();
        var member = _workspaceMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return;

        if (member.Role == WorkspaceRole.Owner && _workspaceMembers.Count(m => m.Role == WorkspaceRole.Owner) <= 1)
            throw new BusinessRuleException("Cannot remove the last owner of the workspace.");

        _workspaceMembers.Remove(member);
        AddDomainEvent(new WorkspaceMemberRemovedEvent(Id, member.Id, removedBy));
    }

    public void ChangeMemberRole(Guid userId, WorkspaceRole newRole, Guid updatedBy)
    {
        EnsureNotDeleted();
        var member = _workspaceMembers.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new BusinessRuleException("Member not found.");

        if (member.Role == WorkspaceRole.Owner && newRole != WorkspaceRole.Owner && _workspaceMembers.Count(m => m.Role == WorkspaceRole.Owner) <= 1)
            throw new BusinessRuleException("Cannot downgrade the role of the last owner.");

        var oldRole = member.Role;
        member.ChangeRole(newRole, updatedBy);
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, member.Id, oldRole, newRole, updatedBy));
    }
}
