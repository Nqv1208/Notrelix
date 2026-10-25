using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

namespace Notrelix.Application.Features.Automation.Executions.Services;

/// <summary>
/// TAC-AI-011A — the production Automation MoveItem executor. Invoked from a
/// durable <see cref="AutomationMoveItemRequestedV1"/> intent, it loads the
/// execution (the process state reference), parses the rule's MoveItem
/// configuration, and invokes the Automation→Work port with the execution id
/// as the target OperationId and the principal propagated from the intent.
/// Lifecycle stays Application-owned: success/failure/retry progressions are
/// decided here from the target outcome, never inside the consumer or the
/// Domain.
/// </summary>
public sealed class AutomationMoveItemUseCase
{
    private readonly IAutomationDbContext _context;
    private readonly IWorkActionPort _workActions;
    private readonly IDateTimeProvider _clock;
    private readonly IOrderingConflictClassifier _orderingConflicts;
    private readonly ICorrelationContext? _correlation;

    internal const string NoExecutorReason = "The rule has no executor and cannot run; recreate it.";
    internal const string ExecutorForbiddenReason = "The rule's executor no longer has permission to perform the action.";
    internal const string ExecutorNotFoundReason = "The rule's target no longer exists or is not visible to its executor.";

    public AutomationMoveItemUseCase(
        IAutomationDbContext context,
        IWorkActionPort workActions,
        IDateTimeProvider clock,
        IOrderingConflictClassifier orderingConflicts,
        ICorrelationContext? correlation = null)
    {
        _correlation = correlation;
        _context = context;
        _workActions = workActions;
        _clock = clock;
        _orderingConflicts = orderingConflicts;
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

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == message.RuleId, cancellationToken);

        // The execution lifecycle only permits Fail from Running, so every
        // terminal pre-execution defect starts the execution first — the
        // failure is recorded against a started, then failed, state machine.
        var startAndFail = async (string error) =>
        {
            execution.Start(_clock.UtcNow);
            execution.Fail(error, _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        };

        if (rule is null)
        {
            return await startAndFail("Automation rule no longer exists.");
        }

        if (rule.Status != AutomationRuleStatus.Active)
        {
            return await startAndFail("Automation rule is not active.");
        }

        // AU-1: the action moves the item the trigger fact was about; a rule made with a
        // fixed item (before rules were board-scoped) keeps moving that one.
        Guid itemId;
        Guid targetGroupId;
        if (message.ItemId is { } triggeringItemId && triggeringItemId != Guid.Empty)
        {
            itemId = triggeringItemId;
            if (!AutomationMoveItemConfiguration.TryParseTarget(rule.Configuration.Action.Configuration, out targetGroupId))
            {
                return await startAndFail("MoveItem action configuration is invalid.");
            }
        }
        else if (!AutomationMoveItemConfiguration.TryParse(rule.Configuration.Action.Configuration, out itemId, out targetGroupId))
        {
            return await startAndFail("MoveItem action configuration is invalid.");
        }

        // AUT-020: the rule runs as its bound executor. A rule with no executor
        // (it predates the binding and had no creator on record) is not
        // executable: fail closed and stop the rule.
        if (!rule.HasExecutor)
        {
            rule.DisableAutomatically(NoExecutorReason, _clock.UtcNow);
            return await startAndFail("The rule has no executor; the automation cannot execute.");
        }

        var executorUserId = rule.ExecutorUserId!.Value;

        if (message.ActorUserId is null || message.ActorUserId == Guid.Empty)
        {
            return await startAndFail("The trigger fact carried no trusted actor; the automation cannot execute.");
        }

        var principal = new AutomationPrincipal(
            message.AccountId!.Value,
            executorUserId,
            message.WorkspaceId!.Value,
            message.CorrelationId,
            message.CausationId);

        execution.Start(_clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        // AUT-022: every fact the target action commits carries this execution as
        // its causation, so the rule evaluator can recognise Automation-caused
        // facts and bound chains/self-triggers. The context is scoped to this
        // delivery.
        _correlation?.Set(message.CorrelationId, execution.Id);
        _correlation?.SetAutomationRule(rule.Id);

        try
        {
            // AUT-020: re-authorize the executor against the target now. Losing the
            // permission (or the visibility of the target) stops the rule with a
            // recorded reason; nothing is moved. Technical failures of the check
            // stay retryable like the move itself.
            try
            {
                // AU-2: a board-scoped rule only ever moves items of its own board.
                if (rule.BoardId is { } ruleBoardId)
                {
                    try
                    {
                        await _workActions.EnsureItemOnBoardAsync(itemId, ruleBoardId, principal, cancellationToken);
                    }
                    catch (NotFoundException)
                    {
                        execution.Fail("The triggering item is not on the rule's board.", _clock.UtcNow);
                        await _context.SaveChangesAsync(cancellationToken);
                        return true;
                    }
                }

                await _workActions.EnsureMoveItemAllowedAsync(itemId, targetGroupId, principal, cancellationToken);
            }
            catch (ForbiddenException)
            {
                return await StopRuleAsync(rule, execution, ExecutorForbiddenReason,
                    "The rule's executor no longer has permission to perform this action.", cancellationToken);
            }
            catch (NotFoundException)
            {
                return await StopRuleAsync(rule, execution, ExecutorNotFoundReason,
                    "The rule's target is not available to its executor.", cancellationToken);
            }

            var result = await _workActions.MoveItemAsync(
                itemId,
                targetGroupId,
                message.ExecutionId,
                principal,
                cancellationToken);

            execution.Succeed(_clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (WorkItemOperationConflictException)
        {
            // The target's OperationId dedup reports a conflicting payload for
            // the same execution id — an automation configuration defect, not
            // a transient condition.
            execution.Fail("Target action rejected the operation payload for this execution.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is BusinessRuleException or ForbiddenException or NotFoundException)
        {
            // Target business rejection (rule violation, not found,
            // unauthorized, scope mismatch) is a terminal business failure
            // of the automation. Retrying cannot repair a deterministic
            // target-owned rejection, so the execution fails closed instead
            // of entering the technical retry loop.
            execution.Fail("Target action business rejection.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
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
}
