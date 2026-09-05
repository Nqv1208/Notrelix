namespace Notrelix.Application.Features.Automation.Ports.WorkManagement;

/// <summary>
/// Automation-owned execution principal for a target Work Management action:
/// the workflow supplies the exact user/scope the target should enforce.
/// </summary>
public sealed record AutomationPrincipal(
    Guid ExecutorUserId,
    Guid WorkspaceId);

/// <summary>
/// Automation-owned semantic port for driving a Work Management item action.
/// The target context owns the mutation; this port carries Automation
/// vocabulary only and is implemented by a runtime adapter.
/// </summary>
public interface IWorkActionPort
{
    /// <summary>
    /// Moves the item into the target group as part of an automation execution.
    /// Target business failures are raised as target-owned errors; only
    /// technical failures are retryable by the delivery mechanism.
    /// </summary>
    Task<WorkActionResult> MoveItemAsync(
        Guid itemId,
        Guid targetGroupId,
        Guid executionId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken);
}

/// <summary>
/// Outcome of a target Work action: placement when the move succeeded.
/// </summary>
public sealed record WorkActionResult(
    Guid ItemId,
    Guid GroupId,
    string Position);
