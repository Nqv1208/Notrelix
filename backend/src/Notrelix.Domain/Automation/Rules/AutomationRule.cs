namespace Notrelix.Domain.Automation.Rules;

public class AutomationRule : AggregateRoot, IWorkspaceScoped
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
        rule.AddDomainEvent(new AutomationRuleCreatedDomainEvent(accountId, workspaceId, rule.Id, rule.Name, createdBy, createdAt));

        return rule;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Active) return;
        EnsureNotDeleted();

        Status = AutomationRuleStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleEnabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == AutomationRuleStatus.Disabled) return;
        EnsureNotDeleted();

        Status = AutomationRuleStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleDisabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void UpdateConfiguration(AutomationConfiguration config, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(config);

        if (Configuration == config) return;

        Configuration = config;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationConfigurationChangedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = AutomationRuleStatus.Disabled;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = AutomationRuleStatus.Draft;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new AutomationRuleRestoredDomainEvent(AccountId, WorkspaceId, Id, Name, restoredBy, restoredAt));
    }
}
