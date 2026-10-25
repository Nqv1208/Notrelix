using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Events.Automation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.Features.Automation.Events;

/// <summary>
/// Evaluates the runtime-reachable Work automation triggers ("ItemAssigned",
/// "ItemMovedToGroup", "ItemCreated") and, for each matching active rule with
/// a dispatchable action, creates an <see cref="AutomationExecution"/> and
/// stages a durable outbox intent. The intent routes explicitly by action
/// type: "MoveItem" rules stage the Work target-action dispatch (executed
/// through the Automation→Work port); "Webhook" rules stage the n8n
/// dispatch; any other configured action type fails the execution closed —
/// it is never silently rerouted to the wrong dispatcher (debt:
/// M8-AUTOMATION-ACTION-PARITY). Both dispatch intents commit atomically
/// with the consumer's transaction; the HTTP or target-action dispatch is
/// performed by a MassTransit consumer after commit. No process-local queue.
/// </summary>
public sealed class N8nAutomationRuleEvaluator
{
    private readonly IAutomationDbContext _context;
    private readonly IIntegrationEventCollector _events;
    private readonly IDateTimeProvider _clock;
    private readonly AutomationGuardOptions _guards;
    private readonly ILogger<N8nAutomationRuleEvaluator> _logger;

    private const string NoExecutorReason = "The rule has no executor and cannot run; recreate it.";

    public N8nAutomationRuleEvaluator(
        IAutomationDbContext context,
        IIntegrationEventCollector events,
        IDateTimeProvider clock,
        IOptions<AutomationGuardOptions>? guards = null,
        ILogger<N8nAutomationRuleEvaluator>? logger = null)
    {
        _context = context;
        _events = events;
        _clock = clock;
        _guards = guards?.Value ?? new AutomationGuardOptions();
        _logger = logger ?? NullLogger<N8nAutomationRuleEvaluator>.Instance;
    }

    public async Task ExecuteAsync(
        AutomationTriggerContext trigger,
        CancellationToken cancellationToken)
    {
        // The integration event carries authoritative account/workspace scope;
        // no resource re-resolution is required at this stage.
        var workspaceId = trigger.WorkspaceId;
        var accountId = trigger.AccountId;

        var rules = await _context.AutomationRules
            .Where(rule =>
                rule.WorkspaceId == workspaceId &&
                rule.AccountId == accountId &&
                rule.Status == AutomationRuleStatus.Active)
            .ToListAsync(cancellationToken);

        // AUT-022: when the fact was caused by an Automation execution, resolve
        // that execution's ancestry (rule ids + depth). A fact without causation
        // information, or one whose causation is not an execution of this
        // workspace, is not Automation-caused and behaves as before.
        var ancestry = await ResolveAncestryAsync(trigger, cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.Configuration.Trigger.Type != trigger.TriggerType)
            {
                continue;
            }

            // AU-2: a board-scoped rule triggers only for facts of its board, and only when
            // the fact passes the rule's trigger filter (the group the item moved to).
            if (rule.BoardId is { } ruleBoardId && trigger.BoardId != ruleBoardId)
            {
                continue;
            }

            if (!PassesTriggerFilter(rule, trigger))
            {
                continue;
            }

            // AUT-020: a rule with no bound executor (it predates the binding and
            // had no creator on record) can never run. Fail closed and stop it at
            // its first evaluation, visibly, instead of running as someone else.
            if (!rule.HasExecutor)
            {
                rule.DisableAutomatically(NoExecutorReason, _clock.UtcNow);
                _logger.LogWarning(
                    "Automation rule {RuleId} was disabled: it has no executor.", rule.Id);
                continue;
            }

            var exists = await _context.AutomationExecutions
                .AsNoTracking()
                .AnyAsync(execution =>
                    execution.RuleId == rule.Id &&
                    execution.TriggerId == trigger.SourceEventId,
                    cancellationToken);

            if (exists) continue;

            if (ancestry is not null && IsSuppressedByAncestry(rule, ancestry))
            {
                continue;
            }

            if (await IsThrottledOrTrippedAsync(rule, trigger, cancellationToken))
            {
                continue;
            }

            var execution = AutomationExecution.Create(
                accountId,
                workspaceId,
                rule.Id,
                trigger.SourceEventId,
                trigger.OccurredAt,
                ancestry?.ParentExecutionId,
                ancestry is null ? 0 : ancestry.ParentDepth + 1);

            _context.AutomationExecutions.Add(execution);

            switch (rule.Configuration.Action.Type)
            {
                case "MoveItem":
                    _events.Add(new AutomationMoveItemRequestedV1(
                        Guid.CreateVersion7(),
                        execution.Id,
                        rule.Id,
                        accountId,
                        workspaceId,
                        trigger.ActorUserId,
                        _clock.UtcNow,
                        trigger.CorrelationId,
                        trigger.CausationId,
                        // AU-1: a board-scoped rule moves the item the fact was about.
                        rule.BoardId is null ? null : trigger.ItemId));
                    break;

                case "Webhook":
                    _events.Add(new N8nDispatchRequestedV1(
                        Guid.CreateVersion7(),
                        execution.Id,
                        rule.Id,
                        accountId,
                        workspaceId,
                        _clock.UtcNow,
                        trigger.CorrelationId,
                        trigger.SourceEventId,
                        trigger.CausationId));
                    break;

                default:
                    // Fail closed: an action type without a runtime executor is
                    // never silently rerouted to another dispatcher. The rule
                    // stays a valid configuration (vocabulary is broader than
                    // the M8 runtime) but the staged execution terminates
                    // visibly instead of dispatching to the wrong channel
                    // (debt: M8-AUTOMATION-ACTION-PARITY).
                    var failedAt = _clock.UtcNow;
                    execution.Start(failedAt);
                    execution.Fail(
                        $"Action type '{rule.Configuration.Action.Type}' has no runtime executor (M8-AUTOMATION-ACTION-PARITY).",
                        failedAt);
                    break;
            }
        }
    }

    /// <summary>
    /// AU-2: the trigger's runtime filter. <c>{"groupId":"…"}</c> matches when the item moved
    /// into that group; a trigger without a filter matches every fact of its type.
    /// </summary>
    private static bool PassesTriggerFilter(AutomationRule rule, AutomationTriggerContext trigger)
    {
        var configuration = rule.Configuration.Trigger.Configuration;
        if (string.IsNullOrWhiteSpace(configuration)) return true;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(configuration);
            var root = document.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object) return true;

            return Matches(root, "groupId", trigger.GroupId)
                && Matches(root, "fromGroupId", trigger.FromGroupId);
        }
        catch (System.Text.Json.JsonException)
        {
            return true;
        }

        static bool Matches(System.Text.Json.JsonElement root, string name, Guid? actual) =>
            !root.TryGetProperty(name, out var wanted)
            || wanted.ValueKind != System.Text.Json.JsonValueKind.String
            || !Guid.TryParse(wanted.GetString(), out var wantedId)
            || actual == wantedId;
    }

    /// <summary>The chain of Automation executions that caused the trigger fact.</summary>
    private sealed record ExecutionAncestry(
        Guid ParentExecutionId,
        int ParentDepth,
        IReadOnlySet<Guid> RuleIds);

    private async Task<ExecutionAncestry?> ResolveAncestryAsync(
        AutomationTriggerContext trigger,
        CancellationToken cancellationToken)
    {
        if (trigger.CausationId is not { } causationId)
        {
            return null;
        }

        var parent = await _context.AutomationExecutions
            .AsNoTracking()
            .Where(e => e.Id == causationId && e.WorkspaceId == trigger.WorkspaceId)
            .Select(e => new { e.Id, e.RuleId, e.ChainDepth, e.CausationExecutionId })
            .FirstOrDefaultAsync(cancellationToken);

        if (parent is null)
        {
            return null;
        }

        // Walk the causation chain (bounded by the depth cap) so a rule that
        // already appears anywhere upstream is recognised, not only the
        // immediate parent: A -> B -> A is a loop even though B != A.
        var ruleIds = new HashSet<Guid> { parent.RuleId };
        var next = parent.CausationExecutionId;
        for (var hop = 0; next is { } id && hop < _guards.MaxChainDepth; hop++)
        {
            var ancestor = await _context.AutomationExecutions
                .AsNoTracking()
                .Where(e => e.Id == id && e.WorkspaceId == trigger.WorkspaceId)
                .Select(e => new { e.RuleId, e.CausationExecutionId })
                .FirstOrDefaultAsync(cancellationToken);
            if (ancestor is null)
            {
                break;
            }

            ruleIds.Add(ancestor.RuleId);
            next = ancestor.CausationExecutionId;
        }

        return new ExecutionAncestry(parent.Id, parent.ChainDepth, ruleIds);
    }

    private bool IsSuppressedByAncestry(AutomationRule rule, ExecutionAncestry ancestry)
    {
        if (ancestry.RuleIds.Contains(rule.Id))
        {
            _logger.LogWarning(
                "Automation rule {RuleId} was not triggered by a fact its own chain caused (execution {ParentExecutionId}).",
                rule.Id, ancestry.ParentExecutionId);
            return true;
        }

        if (ancestry.ParentDepth + 1 > _guards.MaxChainDepth)
        {
            _logger.LogWarning(
                "Automation rule {RuleId} was not triggered: the chain from execution {ParentExecutionId} reached depth {MaxChainDepth}.",
                rule.Id, ancestry.ParentExecutionId, _guards.MaxChainDepth);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Per-rule runaway protection: true when the rule must not start another
    /// execution for this fact. A rule whose most recent finished executions
    /// (since it was last enabled or edited) all failed is disabled; a rule
    /// that already started its allowance of executions in the window is
    /// throttled for this fact.
    /// </summary>
    private async Task<bool> IsThrottledOrTrippedAsync(
        AutomationRule rule,
        AutomationTriggerContext trigger,
        CancellationToken cancellationToken)
    {
        var since = rule.UpdatedAt ?? rule.CreatedAt;
        var recent = await _context.AutomationExecutions
            .AsNoTracking()
            .Where(e => e.RuleId == rule.Id
                && e.CreatedAt > since
                && (e.Status == AutomationExecutionStatus.Succeeded
                    || e.Status == AutomationExecutionStatus.Failed
                    || e.Status == AutomationExecutionStatus.Cancelled))
            .OrderByDescending(e => e.CreatedAt)
            .Take(_guards.ConsecutiveFailureLimit)
            .Select(e => e.Status)
            .ToListAsync(cancellationToken);

        if (recent.Count >= _guards.ConsecutiveFailureLimit
            && recent.All(status => status == AutomationExecutionStatus.Failed))
        {
            rule.DisableAutomatically(
                $"Disabled after {_guards.ConsecutiveFailureLimit} consecutive failed executions.",
                _clock.UtcNow);
            _logger.LogWarning(
                "Automation rule {RuleId} was disabled after {Failures} consecutive failed executions.",
                rule.Id, _guards.ConsecutiveFailureLimit);
            return true;
        }

        var windowStart = trigger.OccurredAt - TimeSpan.FromSeconds(_guards.RateWindowSeconds);
        var started = await _context.AutomationExecutions
            .AsNoTracking()
            .CountAsync(e => e.RuleId == rule.Id && e.CreatedAt > windowStart, cancellationToken);

        if (started >= _guards.MaxExecutionsPerWindow)
        {
            _logger.LogWarning(
                "Automation rule {RuleId} was throttled: {Started} executions within {WindowSeconds}s.",
                rule.Id, started, _guards.RateWindowSeconds);
            return true;
        }

        return false;
    }

    public Task ExecuteAsync(
        BoardItemMemberAssignedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountIdValue,
            integrationEvent.WorkspaceIdValue,
            "ItemAssigned",
            integrationEvent.SourceEventId ?? integrationEvent.EventId,
            integrationEvent.AssignedBy,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt), cancellationToken);
    }

    public Task ExecuteAsync(
        BoardItemMovedIntegrationEventV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        var workspaceId = integrationEvent.WorkspaceId
            ?? throw new InvalidOperationException(
                "The moved-item fact carried no workspace scope; the tenant envelope is authoritative.");

        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountId
                ?? throw new InvalidOperationException(
                    "The moved-item fact carried no account scope; the tenant envelope is authoritative."),
            workspaceId,
            "ItemMovedToGroup",
            integrationEvent.EventId,
            integrationEvent.ActorUserId,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt,
            integrationEvent.BoardId,
            integrationEvent.ItemId,
            integrationEvent.NewGroupId,
            integrationEvent.OldGroupId), cancellationToken);
    }

    public Task ExecuteAsync(
        BoardItemCreatedIntegrationEventV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        var workspaceId = integrationEvent.WorkspaceId
            ?? throw new InvalidOperationException(
                "The created-item fact carried no workspace scope; the tenant envelope is authoritative.");

        return ExecuteAsync(new AutomationTriggerContext(
            integrationEvent.AccountId
                ?? throw new InvalidOperationException(
                    "The created-item fact carried no account scope; the tenant envelope is authoritative."),
            workspaceId,
            "ItemCreated",
            integrationEvent.EventId,
            integrationEvent.ActorUserId,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.OccurredAt,
            integrationEvent.BoardId,
            integrationEvent.ItemId), cancellationToken);
    }
}
