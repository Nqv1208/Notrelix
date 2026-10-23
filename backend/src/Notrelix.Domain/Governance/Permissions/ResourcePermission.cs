using Notrelix.Domain.Governance.Permissions.Events;
namespace Notrelix.Domain.Governance.Permissions;

public class ResourcePermission : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    private bool _suppressDeleteEvent;

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceKind ResourceKind { get; private set; }
    public Guid ResourceId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public PermissionLevel Level { get; private set; }
    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;
    public string ConditionJson { get; private set; } = "{}";
    public int Priority { get; private set; } = 100;

    private ResourcePermission() : base() { }

    public static ResourcePermission Grant(
        Guid accountId,
        Guid workspaceId,
        ResourceKind resourceKind,
        Guid resourceId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        PermissionLevel level,
        Guid grantedBy,
        DateTimeOffset grantedAt)
    {
        // Certified core (WG-DEBT-003/005): a resource entry is Allow-only, has no expiry
        // or condition, and is held by a User or a Team. Other subject kinds are never
        // evaluated by the access-facts query, so refusing them here keeps dead rows out.
        if (subjectType is not (PermissionSubjectType.User or PermissionSubjectType.Team))
            throw new BusinessRuleException(
                GovernanceRuleCodes.Governance_ResourcePermission_SubjectTypeNotSupported,
                $"A resource permission can only be granted to a User or a Team, not '{subjectType}'.");

        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(resourceId);
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(accountId);

        var permission = new ResourcePermission
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ResourceKind = resourceKind,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level
        };

        permission.SetAuditOnCreate(grantedBy, grantedAt);
        permission.RaiseDomainEvent(new ResourcePermissionGrantedDomainEvent(
            accountId, workspaceId, permission.Id, resourceKind, resourceId, subjectType, subjectId, level, grantedBy, grantedAt));

        return permission;
    }

    public void ChangeLevel(PermissionLevel newLevel, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Level == newLevel) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldLevel = Level;
        Level = newLevel;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ResourcePermissionLevelChangedDomainEvent(AccountId, WorkspaceId, Id, ResourceKind, ResourceId, SubjectType, SubjectId, oldLevel, newLevel, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        IncrementVersion();
        ApplyDeletion(pendingDeletion);
        if (!_suppressDeleteEvent)
            RaiseDomainEvent(new ResourcePermissionDeletedDomainEvent(AccountId, WorkspaceId, Id, ResourceKind, ResourceId, deletedBy, deletedAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();

        _suppressDeleteEvent = true;
        Delete(revokedBy, revokedAt);
        _suppressDeleteEvent = false;
        RaiseDomainEvent(new ResourcePermissionRevokedDomainEvent(AccountId, WorkspaceId, Id, ResourceKind, ResourceId, SubjectType, SubjectId, revokedBy, revokedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        IncrementVersion();
        ApplyRestore(pendingRestore);
        RaiseDomainEvent(new ResourcePermissionRestoredDomainEvent(AccountId, WorkspaceId, Id, ResourceKind, ResourceId, restoredBy, restoredAt));
    }
}
