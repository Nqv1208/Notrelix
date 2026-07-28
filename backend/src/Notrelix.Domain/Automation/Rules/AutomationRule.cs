using Notrelix.Domain.Automation.Rules.Events;
using Notrelix.Domain.Automation.RulesEngine;
namespace Notrelix.Domain.Automation.Rules;

public class AutomationRule : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AutomationRuleStatus Status { get; private set; }
    public AutomationConfiguration Configuration { get; private set; } = null!;

    public bool IsEnabled => Status == AutomationRuleStatus.Active;

    private AutomationRule() : base() { }

    public static AutomationRule Create(
        Guid accountId,
        Guid workspaceId,
        string name,
        AutomationConfiguration configuration,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(configuration);

        var rule = new AutomationRule
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Configuration = configuration,
            Status = AutomationRuleStatus.Draft
        };

        rule.SetAuditOnCreate(createdBy, createdAt);
        rule.RaiseDomainEvent(new AutomationRuleCreatedDomainEvent(accountId, workspaceId, rule.Id, rule.Name, createdBy, createdAt));

        return rule;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == AutomationRuleStatus.Active) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AutomationRuleStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleEnabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == AutomationRuleStatus.Disabled) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AutomationRuleStatus.Disabled;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDisabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void UpdateConfiguration(AutomationConfiguration config, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(config);
        Guard.NotEmpty(updatedBy);

        if (Configuration == config) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Configuration = config;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationConfigurationChangedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        var pending = PrepareAuditUpdate(deletedBy, deletedAt);
        Status = AutomationRuleStatus.Disabled;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        Status = AutomationRuleStatus.Draft;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleRestoredDomainEvent(AccountId, WorkspaceId, Id, Name, restoredBy, restoredAt));
    }
}
