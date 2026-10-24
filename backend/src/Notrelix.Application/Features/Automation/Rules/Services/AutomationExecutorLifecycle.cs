using Notrelix.Application.Features.Automation.Abstractions;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>
/// AUT-020: stops the Active rules a workspace member runs when that member loses
/// the standing the rules depend on (removed, suspended, or demoted). Driven by
/// the Workspaces-owned member facts through thin Automation consumers; Automation
/// mutates only its own rules and never reaches into Workspaces storage. The
/// execution-time re-authorization remains the authority: this is the proactive
/// stop so a rule does not stay Active and misleading until its next trigger.
/// The consumer pipeline owns the commit.
/// </summary>
public sealed class AutomationExecutorLifecycle
{
    public const string RemovedReason = "The rule's executor was removed from the workspace.";
    public const string SuspendedReason = "The rule's executor was suspended in the workspace.";
    public const string DemotedReason = "The rule's executor was demoted in the workspace.";

    private readonly IAutomationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AutomationExecutorLifecycle> _logger;

    public AutomationExecutorLifecycle(
        IAutomationDbContext context,
        IDateTimeProvider clock,
        ILogger<AutomationExecutorLifecycle> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public Task OnMemberRemovedAsync(Guid accountId, Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        DisableExecutorRulesAsync(accountId, workspaceId, userId, RemovedReason, cancellationToken);

    public Task OnMemberSuspendedAsync(Guid accountId, Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        DisableExecutorRulesAsync(accountId, workspaceId, userId, SuspendedReason, cancellationToken);

    /// <summary>
    /// A role change disables only when Workspaces ranks it as a demotion; the
    /// ranking is Workspaces-owned and arrives on the fact.
    /// </summary>
    public Task OnMemberRoleChangedAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        bool isDemotion,
        CancellationToken cancellationToken) =>
        isDemotion
            ? DisableExecutorRulesAsync(accountId, workspaceId, userId, DemotedReason, cancellationToken)
            : Task.CompletedTask;

    private async Task DisableExecutorRulesAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken)
    {
        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.AccountId == accountId &&
                rule.WorkspaceId == workspaceId &&
                rule.ExecutorUserId == userId &&
                rule.Status == AutomationRuleStatus.Active)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var rule in rules)
        {
            rule.DisableAutomatically(reason, now);
            _logger.LogWarning(
                "Automation rule {RuleId} was disabled: {Reason}",
                rule.Id, reason);
        }
    }
}
