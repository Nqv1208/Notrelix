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

    public AutomationMoveItemUseCase(
        IAutomationDbContext context,
        IWorkActionPort workActions,
        IDateTimeProvider clock)
    {
        _context = context;
        _workActions = workActions;
        _clock = clock;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == message.RuleId, cancellationToken);

        if (rule is null)
        {
            execution.Fail("Automation rule no longer exists.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (rule.Status != AutomationRuleStatus.Active)
        {
            execution.Fail("Automation rule is not active.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (!TryParseMoveItemConfiguration(rule.Configuration.Action.Configuration, out var itemId, out var targetGroupId))
        {
            execution.Fail("MoveItem action configuration is invalid.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (message.ActorUserId is null || message.ActorUserId == Guid.Empty)
        {
            execution.Fail("The trigger fact carried no trusted actor; the automation cannot execute.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        execution.Start(_clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await _workActions.MoveItemAsync(
                itemId,
                targetGroupId,
                message.ExecutionId,
                new AutomationPrincipal(
                    message.AccountId!.Value,
                    message.ActorUserId.Value,
                    message.WorkspaceId!.Value,
                    message.CorrelationId,
                    message.CausationId),
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
        catch (BusinessRuleException)
        {
            // Target business rejection (not found, unauthorized, scope
            // mismatch) is a terminal business failure of the automation.
            execution.Fail("Target action business rejection.", _clock.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
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

    private static bool TryParseMoveItemConfiguration(string? configuration, out Guid itemId, out Guid targetGroupId)
    {
        itemId = Guid.Empty;
        targetGroupId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(configuration))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(configuration);
            var root = document.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object
                || !root.TryGetProperty("itemId", out var itemIdElement)
                || !root.TryGetProperty("targetGroupId", out var groupElement)
                || !Guid.TryParse(itemIdElement.GetString(), out itemId)
                || !Guid.TryParse(groupElement.GetString(), out targetGroupId))
            {
                return false;
            }

            return itemId != Guid.Empty && targetGroupId != Guid.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
