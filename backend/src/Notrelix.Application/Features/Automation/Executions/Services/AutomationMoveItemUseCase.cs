using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.DTOs;
using Notrelix.Application.Features.Automation.Ports;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

namespace Notrelix.Application.Features.Automation.Executions.Services;

/// <summary>
/// The production executor of a rule's actions (the move-item dispatch intent name predates
/// rules with several actions). Invoked from a durable <see cref="AutomationMoveItemRequestedV1"/>
/// intent, it loads the execution (the process state reference) and runs the actions of the rule
/// version the execution started with, in order, as the rule's executor: each action goes through
/// the Automation port of the context that owns the change (Work Management for item changes,
/// Collaboration/email for messages), keyed by an operation id derived from the execution and the
/// action's place, so a redelivery never repeats a finished action. Lifecycle stays
/// Application-owned: success/failure/retry progressions are decided here from the target outcome,
/// never inside the consumer or the Domain.
/// </summary>
public sealed class AutomationMoveItemUseCase
{
    private readonly IAutomationDbContext _context;
    private readonly IWorkActionPort _workActions;
    private readonly IDateTimeProvider _clock;
    private readonly IOrderingConflictClassifier _orderingConflicts;
    private readonly ICorrelationContext? _correlation;
    private readonly IAutomationMessagePort? _messages;

    internal const string NoExecutorReason = "The rule has no executor and cannot run; recreate it.";
    internal const string ExecutorForbiddenReason = "The rule's executor no longer has permission to perform the action.";
    internal const string ExecutorNotFoundReason = "The rule's target no longer exists or is not visible to its executor.";

    public AutomationMoveItemUseCase(
        IAutomationDbContext context,
        IWorkActionPort workActions,
        IDateTimeProvider clock,
        IOrderingConflictClassifier orderingConflicts,
        ICorrelationContext? correlation = null,
        IAutomationMessagePort? messages = null)
    {
        _correlation = correlation;
        _messages = messages;
        _context = context;
        _workActions = workActions;
        _clock = clock;
        _orderingConflicts = orderingConflicts;
    }

    /// <summary>The operation id of the action at <paramref name="index"/>; the first keeps the execution id.</summary>
    public static Guid StepOperationId(Guid executionId, int index)
    {
        if (index == 0) return executionId;
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{executionId:N}:{index}"));
        return new Guid(bytes);
    }

    /// <summary>True = the execution reached a terminal state; false = retryable.</summary>
    public async Task<bool> ExecuteAsync(
        AutomationMoveItemRequestedV1 message,
        CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(e => e.Id == message.ExecutionId, cancellationToken);

        // A missing execution is a terminal no-op: the intent references state
        // that no longer exists and redelivery cannot repair that.
        if (execution is null)
        {
            return true;
        }

        // A deleted rule is gone from the lists, but its in-flight executions still finish.
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == message.RuleId, cancellationToken);

        // The execution lifecycle only permits Fail from Running, so every
        // terminal pre-execution defect starts the execution first — the
        // failure is recorded against a started, then failed, state machine.
        var startAndFail = async (string error) =>
        {
            if (execution.Status == AutomationExecutionStatus.Queued)
            {
                execution.Start(_clock.UtcNow);
            }

            execution.Fail(error, _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        };

        if (rule is null)
        {
            return await startAndFail("Automation rule no longer exists.");
        }

        // The version the execution started with; an execution staged before versions existed
        // runs the rule as it is now.
        var snapshot = AutomationRuleSnapshot.TryDeserialize(execution.ConfigurationSnapshot)
            ?? AutomationRuleSnapshot.From(rule);

        // Deleting disables the rule first and lets in-flight executions finish; a rule the
        // platform or a person only disabled stops its queued executions.
        if (rule.Status != AutomationRuleStatus.Active && !rule.IsRemoved)
        {
            return await startAndFail("Automation rule is not active.");
        }

        // AUT-020: the rule runs as the executor of the version it started on. A rule with no
        // executor (it predates the binding and had no creator on record) is not executable:
        // fail closed and stop the rule.
        if (snapshot.ExecutorUserId is not { } executorUserId || executorUserId == Guid.Empty)
        {
            if (!rule.IsRemoved) rule.DisableAutomatically(NoExecutorReason, _clock.UtcNow);
            return await startAndFail("The rule has no executor; the automation cannot execute.");
        }

        if (message.ActorUserId is null || message.ActorUserId == Guid.Empty)
        {
            return await startAndFail("The trigger fact carried no trusted actor; the automation cannot execute.");
        }

        // AU-1: the actions act on the item the trigger fact was about; a rule made with a
        // fixed item (before rules were board-scoped) keeps moving that one.
        Guid? itemId = message.ItemId is { } triggering && triggering != Guid.Empty ? triggering : null;
        var actions = snapshot.Actions;
        if (itemId is null && !(actions.Count == 1 && actions[0].Type == AutomationActionConfigs.Types.MoveItem
                && AutomationMoveItemConfiguration.TryParse(actions[0].Configuration, out _, out _)))
        {
            return await startAndFail("The trigger fact carried no item for the rule's actions.");
        }

        var principal = new AutomationPrincipal(
            message.AccountId!.Value,
            executorUserId,
            message.WorkspaceId!.Value,
            message.CorrelationId,
            message.CausationId);

        if (execution.Status == AutomationExecutionStatus.Queued)
        {
            execution.Start(_clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // AUT-022: every fact the target action commits carries this execution as
        // its causation, so the rule evaluator can recognise Automation-caused
        // facts and bound chains/self-triggers. The context is scoped to this
        // delivery.
        _correlation?.Set(message.CorrelationId, execution.Id);
        _correlation?.SetAutomationRule(rule.Id);

        var steps = (AutomationExecutionProgress.Read(execution.LastResponse) ?? []).ToList();
        var index = 0;
        try
        {
            // AU-2: a board-scoped rule only ever acts on items of its own board.
            if (snapshot.BoardId is { } ruleBoardId && itemId is { } boardItem)
            {
                try
                {
                    await _workActions.EnsureItemOnBoardAsync(boardItem, ruleBoardId, principal, cancellationToken);
                }
                catch (NotFoundException)
                {
                    execution.Fail("The triggering item is not on the rule's board.", _clock.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }
            }

            for (index = 0; index < actions.Count; index++)
            {
                // A redelivery resumes after the actions that already finished.
                if (steps.Any(step => step.Index == index && step.Status == AutomationExecutionProgress.Succeeded))
                {
                    continue;
                }

                var action = actions[index];
                var operationId = StepOperationId(message.ExecutionId, index);
                await RunActionAsync(action, itemId, operationId, principal, snapshot, cancellationToken);

                steps.RemoveAll(step => step.Index == index);
                steps.Add(new AutomationExecutionStepDto(index, action.Type, AutomationExecutionProgress.Succeeded, null));
                execution.RecordProgress(AutomationExecutionProgress.Write(steps));
            }

            execution.Succeed(_clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (ForbiddenException) when (!rule.IsRemoved)
        {
            // AUT-020: re-authorize the executor against the target now. Losing the
            // permission (or the visibility of the target) stops the rule with a
            // recorded reason; the failing action changed nothing.
            return await StopRuleAsync(rule, execution, ExecutorForbiddenReason,
                "The rule's executor no longer has permission to perform this action.", cancellationToken);
        }
        catch (NotFoundException) when (!rule.IsRemoved)
        {
            return await StopRuleAsync(rule, execution, ExecutorNotFoundReason,
                "The rule's target is not available to its executor.", cancellationToken);
        }
        catch (WorkItemOperationConflictException)
        {
            // The target's OperationId dedup reports a conflicting payload for
            // the same execution id — an automation configuration defect, not
            // a transient condition.
            return await FailStepAsync(execution, steps, actions, index,
                "Target action rejected the operation payload for this execution.", cancellationToken);
        }
        catch (Exception ex) when (ex is BusinessRuleException or Notrelix.Domain.Common.Exceptions.BusinessRuleException or ForbiddenException or NotFoundException or ActionRejectedException)
        {
            // Target business rejection (rule violation, not found,
            // unauthorized, scope mismatch) is a terminal business failure
            // of the automation. Retrying cannot repair a deterministic
            // target-owned rejection, so the execution fails closed instead
            // of entering the technical retry loop.
            return await FailStepAsync(execution, steps, actions, index,
                "Target action business rejection.", cancellationToken);
        }
        catch (Exception ex) when (_orderingConflicts.IsOrderingConflict(ex))
        {
            // WM-V3-ORDER-005: a named ordering guard rejected the move at commit.
            // The PostgreSQL transaction is aborted and still tracks the colliding
            // change, so nothing more may be written here. Propagate the stable
            // conflict: the consumer transaction rolls back and redelivery
            // re-resolves placement under the scope lock in a new transaction.
            throw new ConflictException(
                "The position was taken by a concurrent change. Redelivery re-resolves placement.",
                IOrderingConflictClassifier.ConflictCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Technical target failure: keep the execution retryable so the
            // delivery mechanism redelivers the durable intent under the
            // stable execution id.
            execution.RecordRetryableDispatchFailure("Target action technical failure.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task RunActionAsync(
        AutomationActionSnapshot action,
        Guid? itemId,
        Guid operationId,
        AutomationPrincipal principal,
        AutomationRuleSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case AutomationActionConfigs.Types.MoveItem:
                await MoveAsync(action, itemId, operationId, principal, cancellationToken);
                break;

            case AutomationActionConfigs.Types.ChangeStatus:
                if (!AutomationActionConfigs.TryParseChangeStatus(action.Configuration, out var optionId))
                    throw new ActionRejectedException("ChangeStatus action configuration is invalid.");
                await _workActions.ChangeStatusAsync(itemId!.Value, optionId, operationId, principal, cancellationToken);
                break;

            case AutomationActionConfigs.Types.UpdateField:
                if (!AutomationActionConfigs.TryParseUpdateField(action.Configuration, out var fieldId, out var valueJson))
                    throw new ActionRejectedException("UpdateField action configuration is invalid.");
                await _workActions.SetFieldValueAsync(itemId!.Value, fieldId, valueJson, operationId, principal, cancellationToken);
                break;

            case AutomationActionConfigs.Types.AssignPerson:
                if (!AutomationActionConfigs.TryParseAssign(action.Configuration, out var assignee))
                    throw new ActionRejectedException("AssignPerson action configuration is invalid.");
                await _workActions.AssignMemberAsync(itemId!.Value, assignee, operationId, principal, cancellationToken);
                break;

            case AutomationActionConfigs.Types.NotifyMember:
                {
                    if (!AutomationActionConfigs.TryParseNotify(action.Configuration, out var notified, out var text))
                        throw new ActionRejectedException("NotifyMember action configuration is invalid.");
                    var messages = _messages ?? throw new ActionRejectedException("Notifications are not available.");
                    await _workActions.EnsureWorkspaceMemberAsync(notified, principal, cancellationToken);
                    var facts = snapshot.BoardId is { } board && itemId is { } item
                        ? await _workActions.GetItemFactsAsync(item, board, principal, cancellationToken)
                        : null;
                    await messages.NotifyMemberAsync(
                        principal.AccountId, principal.WorkspaceId, notified, itemId, principal.ExecutorUserId,
                        operationId, text ?? snapshot.Name, facts?.Name, cancellationToken);
                    break;
                }

            case AutomationActionConfigs.Types.SendEmail:
                {
                    if (!AutomationActionConfigs.TryParseEmail(action.Configuration, out var recipient, out var subject, out var body))
                        throw new ActionRejectedException("SendEmail action configuration is invalid.");
                    var messages = _messages ?? throw new ActionRejectedException("Email is not available.");
                    await _workActions.EnsureWorkspaceMemberAsync(recipient, principal, cancellationToken);
                    if (!await messages.SendEmailAsync(
                            principal.AccountId, principal.WorkspaceId, recipient, operationId, subject, body, cancellationToken))
                        throw new ActionRejectedException("The recipient has no email address on record.");
                    break;
                }

            default:
                throw new ActionRejectedException($"Action type '{action.Type}' has no runtime executor.");
        }
    }

    private async Task MoveAsync(
        AutomationActionSnapshot action,
        Guid? itemId,
        Guid operationId,
        AutomationPrincipal principal,
        CancellationToken cancellationToken)
    {
        Guid movedItem;
        Guid targetGroupId;
        if (itemId is { } triggering)
        {
            movedItem = triggering;
            if (!AutomationMoveItemConfiguration.TryParseTarget(action.Configuration, out targetGroupId))
                throw new ActionRejectedException("MoveItem action configuration is invalid.");
        }
        else if (!AutomationMoveItemConfiguration.TryParse(action.Configuration, out movedItem, out targetGroupId))
        {
            throw new ActionRejectedException("MoveItem action configuration is invalid.");
        }

        await _workActions.EnsureMoveItemAllowedAsync(movedItem, targetGroupId, principal, cancellationToken);
        await _workActions.MoveItemAsync(movedItem, targetGroupId, operationId, principal, cancellationToken);
    }

    private async Task<bool> FailStepAsync(
        AutomationExecution execution,
        List<AutomationExecutionStepDto> steps,
        IReadOnlyList<AutomationActionSnapshot> actions,
        int index,
        string error,
        CancellationToken cancellationToken)
    {
        if (index < actions.Count)
        {
            steps.RemoveAll(step => step.Index == index);
            steps.Add(new AutomationExecutionStepDto(index, actions[index].Type, AutomationExecutionProgress.Failed, error));
            execution.RecordProgress(AutomationExecutionProgress.Write(steps));
        }

        execution.Fail(index < actions.Count ? $"Action {index + 1} ({actions[index].Type}): {error}" : error, _clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> StopRuleAsync(
        AutomationRule rule,
        AutomationExecution execution,
        string ruleReason,
        string executionError,
        CancellationToken cancellationToken)
    {
        rule.DisableAutomatically(ruleReason, _clock.UtcNow);
        execution.Fail(executionError, _clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>An action the executor rejects for its own configuration (never retried).</summary>
    private sealed class ActionRejectedException(string message) : Exception(message);
}
