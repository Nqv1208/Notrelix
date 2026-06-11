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

    public static AutomationRule Create(Guid workspaceId, string name, Guid createdBy)
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

        rule.SetAuditOnCreate(createdBy);
        rule.AddDomainEvent(new AutomationRuleCreatedEvent(workspaceId, rule.Id, rule.Name, createdBy));

        return rule;
    }

    public void Enable(Guid updatedBy)
    {
        if (Status == AutomationRuleStatus.Active) return;
        Status = AutomationRuleStatus.Active;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new AutomationRuleEnabledEvent(Id, updatedBy));
    }

    public void Disable(Guid updatedBy)
    {
        if (Status == AutomationRuleStatus.Disabled) return;
        Status = AutomationRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new AutomationRuleDisabledEvent(Id, updatedBy));
    }

    public void RecordExecution(DateTimeOffset runAt)
    {
        LastRunAt = runAt;
    }
}
