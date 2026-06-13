using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Governance.Permissions;

public class PermissionRule : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string ScopeType { get; private set; } = null!;
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string SubjectType { get; private set; } = null!;
    public Guid? SubjectId { get; private set; }
    public string? SubjectKey { get; private set; }
    public string Action { get; private set; } = null!;
    public PermissionEffect Effect { get; private set; } = PermissionEffect.Allow;
    public string ConditionJson { get; private set; } = "{}";
    public int Priority { get; private set; } = 100;
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string Status { get; private set; } = "Active";

    private PermissionRule() : base() { }

    public static PermissionRule Create(
        Guid workspaceId,
        string scopeType,
        string? resourceType,
        Guid? resourceId,
        string subjectType,
        Guid? subjectId,
        string? subjectKey,
        string action,
        PermissionEffect effect,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? conditionJson = null,
        int priority = 100,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(scopeType);
        Guard.NotNullOrWhiteSpace(subjectType);
        Guard.NotNullOrWhiteSpace(action);

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
            Status = "Active"
        };

        rule.SetAuditOnCreate(createdBy, createdAt);
        rule.AddDomainEvent(new PermissionRuleCreatedDomainEvent(workspaceId, rule.Id, action, createdBy, createdAt));
        return rule;
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = "Disabled";
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new PermissionRuleDisabledDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
        IncrementVersion();
    }

    public bool IsActive(DateTimeOffset now)
    {
        if (Status != "Active" || IsDeleted) return false;
        if (StartsAt.HasValue && now < StartsAt.Value) return false;
        if (ExpiresAt.HasValue && now > ExpiresAt.Value) return false;
        return true;
    }
}
