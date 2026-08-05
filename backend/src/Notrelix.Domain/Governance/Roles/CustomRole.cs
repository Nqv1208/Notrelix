using Notrelix.Domain.Governance.Roles.Events;
namespace Notrelix.Domain.Governance.Roles;

public class CustomRole : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CustomRoleStatus Status { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<CustomRolePermission> _permissions = new();
    public IReadOnlyCollection<CustomRolePermission> Permissions => _permissions.AsReadOnly();

    private CustomRole() : base() { }

    public static CustomRole Create(Guid accountId, Guid workspaceId, string name, string? description, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.MaxLength(description, 500);
        Guard.NotEmpty(accountId);

        var role = new CustomRole
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = CustomRoleStatus.Active
        };

        role.SetAuditOnCreate(createdBy, createdAt);
        role.RaiseDomainEvent(new CustomRoleCreatedDomainEvent(accountId, role.Id, workspaceId, role.Name, createdBy, createdAt));

        return role;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (IsSystem)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_CannotRenameSystem, "Cannot rename a system role.");
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);

        var newName = name.Trim();
        if (Name == newName) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = newName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AddPermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (_permissions.Any(p => p.Action == action))
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Role_PermissionAlreadyAssigned, $"Permission '{action}' is already assigned to this role.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _permissions.Add(CustomRolePermission.Create(Id, action));
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void RemovePermission(string action, Guid updatedBy, DateTimeOffset updatedAt)
    {
        var permission = _permissions.FirstOrDefault(p => p.Action == action);
        if (permission == null) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _permissions.Remove(permission);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void AssignToMember(Guid memberId, Guid assignedBy, DateTimeOffset assignedAt)
    {
        var pending = PrepareAuditUpdate(assignedBy, assignedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleAssignedDomainEvent(AccountId, WorkspaceId, Id, memberId, assignedBy, assignedAt));
    }

    public void RevokeFromMember(Guid memberId, Guid revokedBy, DateTimeOffset revokedAt)
    {
        var pending = PrepareAuditUpdate(revokedBy, revokedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleRevokedDomainEvent(AccountId, WorkspaceId, Id, memberId, revokedBy, revokedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == CustomRoleStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = CustomRoleStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt)
    {
        if (Status != CustomRoleStatus.Archived) return;

        var pending = PrepareAuditUpdate(activatedBy, activatedAt);
        Status = CustomRoleStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CustomRoleActivatedDomainEvent(AccountId, WorkspaceId, Id, activatedBy, activatedAt));
    }
}
