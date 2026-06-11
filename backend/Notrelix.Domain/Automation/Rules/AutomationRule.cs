using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Automation.Rules;

public class AutomationRule : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AutomationRuleStatus Status { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }

    private AutomationRule() : base() { }

    public static AutomationRule Create(Guid workspaceId, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(name);

        var rule = new AutomationRule
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Status = AutomationRuleStatus.Draft
        };

        rule.SetAuditOnCreate(createdBy, createdAt);
        rule.AddDomainEvent(new AutomationRuleCreatedEvent(workspaceId, rule.Id, rule.Name, createdBy, createdAt));

        return rule;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Active) return;
        Status = AutomationRuleStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new AutomationRuleEnabledEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Disabled) return;
        Status = AutomationRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new AutomationRuleDisabledEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void RecordExecution(DateTimeOffset runAt)
    {
        LastRunAt = runAt;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new AutomationRuleDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }
}
