using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Governance.Roles;

public class CustomRole : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CustomRoleStatus Status { get; private set; }

    private readonly List<CustomRolePermission> _permissions = new();
    public IReadOnlyCollection<CustomRolePermission> Permissions => _permissions.AsReadOnly();

    private CustomRole() : base() { }

    public static CustomRole Create(Guid workspaceId, string name, string? description, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var role = new CustomRole
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = CustomRoleStatus.Active
        };

        role.SetAuditOnCreate(createdBy, createdAt);
        role.AddDomainEvent(new CustomRoleCreatedEvent(role.Id, workspaceId, role.Name, createdBy, createdAt));

        return role;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var newName = name.Trim();
        if (Name == newName) return;

        Name = newName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new CustomRoleUpdatedEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AddPermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (_permissions.Any(p => p.Action == action)) return;

        _permissions.Add(CustomRolePermission.Create(Id, action));
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new CustomRoleUpdatedEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void RemovePermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        var permission = _permissions.FirstOrDefault(p => p.Action == action);
        if (permission == null) return;

        _permissions.Remove(permission);
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new CustomRoleUpdatedEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AssignToMember(Guid memberId, Guid assignedBy, DateTimeOffset assignedAt)
    {
        EnsureNotDeleted();
        AddDomainEvent(new CustomRoleAssignedEvent(WorkspaceId, Id, memberId, assignedBy, assignedAt));
    }

    public void RevokeFromMember(Guid memberId, Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        AddDomainEvent(new CustomRoleRevokedEvent(WorkspaceId, Id, memberId, revokedBy, revokedAt));
    }
}
