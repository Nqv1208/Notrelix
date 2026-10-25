namespace Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

/// <summary>
/// Caller-owned operation identity for a WorkManagement public item action.
/// Carries the account/workspace scope, the executor principal, the producer
/// dedup key, and correlation/causation for attribution. The producer
/// authoritatively locates the resource, evaluates the canonical MoveItem
/// decision for the executor, and owns OperationId dedup; retries with the
/// same operation and payload replay one logical mutation, conflicting
/// payloads fail deterministically.
/// </summary>
public sealed record WorkItemActionIdentity(
    Guid OperationId,
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutorUserId,
    Guid? CorrelationId = null,
    Guid? CausationId = null);

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
/// What a rule condition reads about an item: its group, its status option and the raw JSON text of
/// its field values (by field id).
/// </summary>
public sealed record WorkItemFacts(
    Guid GroupId,
    Guid? StatusOptionId,
    string Name,
    IReadOnlyDictionary<Guid, string> FieldValues);

/// <summary>
/// Deterministic failure for reusing an operation id with a different payload.
/// The first execution wins; the conflicting retry never mutates Work state.
/// </summary>
public sealed class WorkItemOperationConflictException(Guid operationId)
    : Exception($"Operation '{operationId}' was already executed with a different payload.");

/// <summary>
/// Producer-owned public target action for WorkManagement item mutations.
/// Owning context: Work Management — callers request the mutation; the
/// producer decides and persists it through the same producer-local use case
/// the HTTP command handler uses, enforcing scope, executor authority, and
/// producer-owned OperationId dedup. Exceptions:
/// <list type="bullet">
/// <item>unknown item (semantic not-found)</item>
/// <item>target group not on the item's board (semantic not-found)</item>
/// <item>workspace scope mismatch or non-member executor (forbidden)</item>
/// <item>conflicting retry of an executed OperationId (deterministic conflict)</item>
/// </list>
/// </summary>
public interface IWorkItemActions
{
    Task<WorkItemMoveResult> MoveItemAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates, without mutating anything, whether the executor may perform
    /// this exact move: the item exists in the declared workspace and is visible
    /// to the executor, the target group is on the item's board, and the canonical
    /// MoveItem decision allows the executor. Same exceptions as
    /// <see cref="MoveItemAsync"/>; used by callers that must prove authority
    /// before they bind an executor to a future move (AUT-020).
    /// </summary>
    Task EnsureMoveItemAllowedAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// The board is in the declared scope and the executor may edit it, and the group, when
    /// given, is on that board (semantic not-found / forbidden otherwise). Used to validate a
    /// board-scoped rule.
    /// </summary>
    Task EnsureBoardGroupAsync(
        WorkItemActionIdentity execution,
        Guid boardId,
        Guid? groupId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the item's status to the option of the board's Status field, as the HTTP status
    /// command would. Deduplicated by <see cref="WorkItemActionIdentity.OperationId"/>.
    /// </summary>
    Task ChangeStatusAsync(WorkItemActionIdentity execution, Guid itemId, Guid optionId, CancellationToken cancellationToken);

    /// <summary>Writes one field value (raw JSON text) of the item, as the HTTP value command would.</summary>
    Task SetFieldValueAsync(WorkItemActionIdentity execution, Guid itemId, Guid fieldId, string valueJson, CancellationToken cancellationToken);

    /// <summary>Assigns the workspace member to the item (a no-op when already assigned).</summary>
    Task AssignMemberAsync(WorkItemActionIdentity execution, Guid itemId, Guid userId, CancellationToken cancellationToken);

    /// <summary>The facts a condition reads about the item (not-found when it is not on the board).</summary>
    Task<WorkItemFacts> GetItemFactsAsync(Guid workspaceId, Guid itemId, Guid boardId, CancellationToken cancellationToken);

    /// <summary>The option belongs to the board's Status field (semantic not-found otherwise).</summary>
    Task EnsureStatusOptionAsync(Guid workspaceId, Guid boardId, Guid optionId, CancellationToken cancellationToken);

    /// <summary>The field is a writable field of the board and the value is valid for it.</summary>
    Task EnsureFieldValueAsync(Guid workspaceId, Guid boardId, Guid fieldId, string valueJson, CancellationToken cancellationToken);

    /// <summary>The user is an active member of the workspace (forbidden otherwise).</summary>
    Task EnsureWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>The item is on the board (semantic not-found otherwise).</summary>
    Task EnsureItemOnBoardAsync(
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        CancellationToken cancellationToken);
}
