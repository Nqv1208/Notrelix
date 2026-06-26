namespace Notrelix.Domain.Governance.Permissions;

public class PermissionRule : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public PermissionScopeType ScopeType { get; private set; }
    public ResourceType? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public PermissionSubjectType SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string? SubjectKey { get; private set; }
    public PermissionAction Action { get; private set; }
    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;
    public string ConditionJson { get; private set; } = "{}";
    public int Priority { get; private set; } = 100;
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public PermissionRuleStatus Status { get; private set; } = PermissionRuleStatus.Active;

    private PermissionRule() : base() { }

    public static PermissionRule Create(
        Guid workspaceId,
        PermissionScopeType scopeType,
        ResourceType? resourceType,
        Guid? resourceId,
        PermissionSubjectType subjectType,
        Guid? subjectId,
        string? subjectKey,
        PermissionAction action,
        PermissionEffect effect,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? conditionJson = null,
        int priority = 100,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);

        var rule = new PermissionRule
        {
            WorkspaceId = workspaceId,
            ScopeType = scopeType,
            ResourceType = resourceType,
            ResourceId = resourceId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            SubjectKey = subjectKey,
            Action = action,
            Effect = effect,
            ConditionJson = conditionJson ?? "{}",
            Priority = priority,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            Status = PermissionRuleStatus.Active
        };

        rule.SetAuditOnCreate(createdBy, createdAt);
        rule.AddDomainEvent(new PermissionRuleCreatedDomainEvent(workspaceId, rule.Id, action.ToString(), createdBy, createdAt));
        return rule;
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = PermissionRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new PermissionRuleDisabledDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new PermissionRuleSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new PermissionRuleRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }

    public bool IsActive(DateTimeOffset now)
    {
        if (Status != PermissionRuleStatus.Active || IsDeleted) return false;
        if (StartsAt.HasValue && now < StartsAt.Value) return false;
        if (ExpiresAt.HasValue && now > ExpiresAt.Value) return false;
        return true;
    }
}
