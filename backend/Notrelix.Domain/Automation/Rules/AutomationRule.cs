using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Automation.Rules;

public class AutomationRule : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AutomationRuleStatus Status { get; private set; }
    public string TriggerEvent { get; private set; } = null!;
    public string ActionType { get; private set; } = null!;
    public string? Configuration { get; private set; }

    public bool IsEnabled => Status == AutomationRuleStatus.Active;

    private AutomationRule() : base() { }

    public static AutomationRule Create(
        Guid workspaceId,
        string name,
        string triggerEvent,
        string actionType,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? configuration = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(triggerEvent);
        Guard.NotNullOrWhiteSpace(actionType);

        var rule = new AutomationRule
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            TriggerEvent = triggerEvent.Trim(),
            ActionType = actionType.Trim(),
            Configuration = configuration,
            Status = AutomationRuleStatus.Draft
        };

        rule.SetAuditOnCreate(createdBy, createdAt);
        rule.AddDomainEvent(new AutomationRuleCreatedEvent(workspaceId, rule.Id, rule.Name, createdBy, createdAt));

        return rule;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Active) return;
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(TriggerEvent) || string.IsNullOrWhiteSpace(ActionType))
            throw new BusinessRuleException("Cannot enable an automation rule without a trigger and at least one action.");

        Status = AutomationRuleStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleEnabledEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Disabled) return;
        EnsureNotDeleted();

        Status = AutomationRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleDisabledEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void UpdateConfiguration(string config)
    {
        EnsureNotDeleted();
        if (Configuration == config) return;
        Configuration = config;
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = AutomationRuleStatus.Disabled;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = AutomationRuleStatus.Draft;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleCreatedEvent(WorkspaceId, Id, Name, restoredBy, restoredAt));
    }
}
