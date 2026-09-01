namespace Notrelix.Application.Features.WorkManagement.Public.Commands;

/// <summary>
/// Caller-owned operation identity for a WorkManagement public item action.
/// Carries correlation/idempotency identity so retries are attributable;
/// dedup ownership stays with the caller (pipeline idempotency or the
/// background execution identity).
/// </summary>
public sealed record WorkItemActionIdentity(
    Guid OperationId,
    Guid WorkspaceId,
    Guid ExecutorUserId);

/// <summary>
/// Request semantic for the producer-owned move-item action. Contains only
/// WorkManagement-owned vocabulary: the item, its target group, and the
/// explicit execution principal/scope supplied by the caller.
/// </summary>
public sealed record WorkItemMoveRequest(
    WorkItemActionIdentity Execution,
    Guid ItemId,
    Guid NewGroupId);

/// <summary>
/// Producer-owned move outcome: the item's resulting placement.
/// </summary>
public sealed record WorkItemMoveResult(
    Guid ItemId,
    Guid GroupId,
    string Position);

/// <summary>
/// Producer-owned public target action for WorkManagement item mutations.
/// Owning context: Work Management — callers request the mutation; the
/// producer decides and persists it through the same producer-local use case
/// the HTTP command handler uses. Exceptions:
/// <list type="bullet">
/// <item>unknown item (semantic not-found)</item>
/// <item>target group not on the item's board (semantic not-found)</item>
/// </list>
/// </summary>
public interface IWorkItemActions
{
    Task<WorkItemMoveResult> MoveItemAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken);
}
