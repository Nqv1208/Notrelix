namespace Notrelix.Domain.Governance.Roles;

public class CustomRole : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CustomRoleStatus Status { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<CustomRolePermission> _permissions = new();
    public IReadOnlyCollection<CustomRolePermission> Permissions => _permissions.AsReadOnly();

    private CustomRole() : base() { }

    public static CustomRole Create(Guid workspaceId, string name, string? description, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.MaxLength(description, 500);

        var role = new CustomRole
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = CustomRoleStatus.Active
        };

        role.SetAuditOnCreate(createdBy, createdAt);
        role.AddDomainEvent(new CustomRoleCreatedDomainEvent(role.Id, workspaceId, role.Name, createdBy, createdAt));

        return role;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsSystem)
            throw new BusinessRuleException("Cannot rename a system role.");
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);

        var newName = name.Trim();
        if (Name == newName) return;

        Name = newName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleUpdatedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AddPermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (_permissions.Any(p => p.Action == action))
            throw new BusinessRuleException($"Permission '{action}' is already assigned to this role.");

        _permissions.Add(CustomRolePermission.Create(Id, action));
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleUpdatedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void RemovePermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        var permission = _permissions.FirstOrDefault(p => p.Action == action);
        if (permission == null) return;

        _permissions.Remove(permission);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleUpdatedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AssignToMember(Guid memberId, Guid assignedBy, DateTimeOffset assignedAt)
    {
        EnsureNotDeleted();
        SetAuditOnUpdate(assignedBy, assignedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleAssignedDomainEvent(WorkspaceId, Id, memberId, assignedBy, assignedAt));
    }

    public void RevokeFromMember(Guid memberId, Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        SetAuditOnUpdate(revokedBy, revokedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleRevokedDomainEvent(WorkspaceId, Id, memberId, revokedBy, revokedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == CustomRoleStatus.Archived) return;

        Status = CustomRoleStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleArchivedDomainEvent(WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt)
    {
        if (Status != CustomRoleStatus.Archived) return;

        Status = CustomRoleStatus.Active;
        SetAuditOnUpdate(activatedBy, activatedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleActivatedDomainEvent(WorkspaceId, Id, activatedBy, activatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (IsSystem)
            throw new BusinessRuleException("Cannot delete a system role.");
        Status = CustomRoleStatus.Archived;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = CustomRoleStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new CustomRoleRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
