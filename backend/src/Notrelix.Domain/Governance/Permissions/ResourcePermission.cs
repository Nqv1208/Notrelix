using Notrelix.Domain.Governance.Permissions.Events;
namespace Notrelix.Domain.Governance.Permissions;

public class ResourcePermission : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    private bool _suppressSoftDeleteEvent;

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
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
        ResourceType resourceType,
        Guid resourceId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        PermissionLevel level,
        PermissionLevel granterLevel,
        Guid grantedBy,
        DateTimeOffset grantedAt,
        PermissionEffect effect = PermissionEffect.Allow,
        string? conditionJson = null,
        int priority = 100)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(resourceId);
        Guard.NotEmpty(subjectId);
        Guard.NotEmpty(accountId);

        if (!PermissionRules.CanGrant(granterLevel, level))
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_Permission_CannotGrantHigherThanGranter, "Cannot grant a permission level higher than the granter's own level.");

        var permission = new ResourcePermission
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Level = level,
            Effect = effect,
            ConditionJson = conditionJson ?? "{}",
            Priority = priority
        };

        permission.SetAuditOnCreate(grantedBy, grantedAt);
        permission.RaiseDomainEvent(new ResourcePermissionGrantedDomainEvent(
            accountId, workspaceId, permission.Id, resourceType, resourceId, subjectType, subjectId, level, grantedBy, grantedAt));

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
        RaiseDomainEvent(new ResourcePermissionLevelChangedDomainEvent(AccountId, WorkspaceId, Id, ResourceType, ResourceId, SubjectType, SubjectId, oldLevel, newLevel, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        IncrementVersion();
        ApplyDeletion(pendingDeletion);
        if (!_suppressSoftDeleteEvent)
            RaiseDomainEvent(new ResourcePermissionSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, ResourceType, ResourceId, deletedBy, deletedAt));
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();

        _suppressSoftDeleteEvent = true;
        SoftDelete(revokedBy, revokedAt);
        _suppressSoftDeleteEvent = false;
        RaiseDomainEvent(new ResourcePermissionRevokedDomainEvent(AccountId, WorkspaceId, Id, ResourceType, ResourceId, SubjectType, SubjectId, revokedBy, revokedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        IncrementVersion();
        ApplyRestore(pendingRestore);
        RaiseDomainEvent(new ResourcePermissionRestoredDomainEvent(AccountId, WorkspaceId, Id, ResourceType, ResourceId, restoredBy, restoredAt));
    }
}
