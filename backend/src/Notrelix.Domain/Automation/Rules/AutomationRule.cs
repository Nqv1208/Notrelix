using Notrelix.Domain.Automation.Rules.Events;
using Notrelix.Domain.Automation.RulesEngine;
namespace Notrelix.Domain.Automation.Rules;

public class AutomationRule : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// AU-2: the board the rule belongs to; it triggers only for facts of that board and
    /// its actions act on the triggering item. Null only for rules made before rules were
    /// board-scoped, which keep running workspace-wide until they are edited.
    /// </summary>
    public Guid? BoardId { get; private set; }

    /// <summary>
    /// The version of the rule's definition a person sees: 1 when created, raised by every edit
    /// (<see cref="Revise"/>). Switching the rule on or off does not change it.
    /// </summary>
    public int ConfigurationVersion { get; private set; } = 1;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AutomationRuleStatus Status { get; private set; }
    public AutomationConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// AUT-020: the user the rule runs as. Bound to the creator at creation and
    /// never silently replaced. Null only for rules that predate the binding and
    /// had no creator on record: such a rule is not executable (fail closed).
    /// </summary>
    public Guid? ExecutorUserId { get; private set; }

    /// <summary>
    /// Why the rule was last disabled by the platform (executor lost standing,
    /// circuit breaker). Null while enabled and after a manual disable.
    /// </summary>
    public string? DisabledReason { get; private set; }

    public bool HasExecutor => ExecutorUserId is { } executor && executor != Guid.Empty;

    public bool IsEnabled => Status == AutomationRuleStatus.Active;

    /// <summary>A deleted rule: gone from the lists, its in-flight executions finish, its history stays readable for a while.</summary>
    public bool IsRemoved => Status == AutomationRuleStatus.Archived;

    private AutomationRule() : base() { }

    public static AutomationRule Create(
        Guid accountId,
        Guid workspaceId,
        string name,
        AutomationConfiguration configuration,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? boardId = null)
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
            BoardId = boardId,
            Name = name.Trim(),
            Configuration = configuration,
            ExecutorUserId = createdBy,
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

        if (Status == AutomationRuleStatus.Active)
            return;

        EnsureNotRemoved();
        AutomationRuleValidator.ValidateForActivation(Name, Configuration);

        // AUT-020: a rule without a bound executor can never run, so it cannot be enabled.
        if (!HasExecutor)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_MustHaveExecutor,
                "Automation rule has no executor and cannot be enabled; recreate it.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AutomationRuleStatus.Active;
        DisabledReason = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleEnabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status is AutomationRuleStatus.Disabled or AutomationRuleStatus.Archived) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AutomationRuleStatus.Disabled;
        DisabledReason = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDisabledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    /// <summary>
    /// Circuit-breaker disable: the platform (not a user) stops the rule after
    /// repeated execution failures. Same state transition as <see cref="Disable"/>
    /// with no human actor; the Disabled fact carries an empty actor. The reason
    /// is recorded on the rule so the stop is visible (AUT-020).
    /// </summary>
    public void DisableAutomatically(string reason, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(reason);
        if (Status != AutomationRuleStatus.Active) return;

        var pending = PrepareAuditUpdate(null, updatedAt);
        Status = AutomationRuleStatus.Disabled;
        DisabledReason = reason.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDisabledDomainEvent(AccountId, WorkspaceId, Id, Guid.Empty, updatedAt));
    }

    public void UpdateConfiguration(AutomationConfiguration configuration, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(configuration);

        if (Configuration == configuration)
            return;

        if (Status == AutomationRuleStatus.Active)
        {
            AutomationRuleValidator.ValidateForActivation(Name, configuration);
        }

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Configuration = configuration;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationConfigurationChangedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    /// <summary>
    /// Edits the rule: a new version. The editor becomes the executor (the rule runs as someone who
    /// holds the permission of its actions now, never as a person who did not choose them).
    /// Executions already queued or running keep the version they started with.
    /// </summary>
    public void Revise(string name, AutomationConfiguration configuration, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(configuration);

        EnsureNotRemoved();
        AutomationRuleValidator.ValidateForActivation(name, configuration);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = name.Trim();
        Configuration = configuration;
        ConfigurationVersion++;
        ExecutorUserId = updatedBy;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationConfigurationChangedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    /// <summary>
    /// Deletes the rule as a person sees it: it stops triggering and leaves the lists. Executions
    /// already queued or running finish on the version they started with.
    /// </summary>
    public void Remove(Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);
        if (IsRemoved) return;

        var pending = PrepareAuditUpdate(removedBy, removedAt);
        Status = AutomationRuleStatus.Archived;
        DisabledReason = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDeletedDomainEvent(AccountId, WorkspaceId, Id, removedBy, removedAt));
    }

    private void EnsureNotRemoved()
    {
        if (IsRemoved)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_MustHaveConfiguration,
                "The automation rule was deleted.");
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AutomationRuleRestoredDomainEvent(AccountId, WorkspaceId, Id, Name, restoredBy, restoredAt));
    }
}
