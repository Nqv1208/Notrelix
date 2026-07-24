using Notrelix.Domain.Governance.Permissions.Events;
namespace Notrelix.Domain.Governance.Permissions;

public class PermissionRule : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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
        Guid accountId,
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
        Guard.NotEmpty(accountId);

        var rule = new PermissionRule
        {
            AccountId = accountId,
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
        rule.RaiseDomainEvent(new PermissionRuleCreatedDomainEvent(accountId, workspaceId, rule.Id, action.ToString(), createdAt));
        return rule;
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == PermissionRuleStatus.Disabled) return;

        Status = PermissionRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        RaiseDomainEvent(new PermissionRuleDisabledDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
        IncrementVersion();
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new PermissionRuleSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new PermissionRuleRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredAt));
    }

    public bool IsActive(DateTimeOffset now)
    {
        if (Status != PermissionRuleStatus.Active || IsDeleted) return false;
        if (StartsAt.HasValue && now < StartsAt.Value) return false;
        if (ExpiresAt.HasValue && now > ExpiresAt.Value) return false;
        return true;
    }
}
