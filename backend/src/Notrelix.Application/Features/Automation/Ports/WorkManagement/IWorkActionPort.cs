namespace Notrelix.Application.Features.Automation.Ports.WorkManagement;

/// <summary>
/// Automation-owned execution principal for a target Work Management action:
/// the workflow supplies the exact account/scope/user the target should
/// enforce, plus the upstream correlation/causation for attribution. When the
/// upstream has no correlation, the target binds the execution id; causation
/// is never fabricated.
/// </summary>
public sealed record AutomationPrincipal(
    Guid AccountId,
    Guid ExecutorUserId,
    Guid WorkspaceId,
    Guid? CorrelationId = null,
    Guid? CausationId = null);

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

    /// <summary>
    /// Proves, without mutating, that the principal's executor may perform this
    /// move now (AUT-020). Raises the target-owned Forbidden/NotFound errors.
    /// </summary>
    Task EnsureMoveItemAllowedAsync(
        Guid itemId,
        Guid targetGroupId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken);

    /// <summary>
    /// AU-2: proves the rule's board is in the principal's workspace and its executor may
    /// edit it, and that the group (when given) is on that board. Raises the target-owned
    /// Forbidden/NotFound errors.
    /// </summary>
    Task EnsureBoardGroupAsync(
        Guid boardId,
        Guid? groupId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken);

    /// <summary>Sets the triggering item's status to an option of its board's Status field.</summary>
    Task ChangeStatusAsync(Guid itemId, Guid optionId, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>Writes a field value (raw JSON text) of the triggering item.</summary>
    Task SetFieldValueAsync(Guid itemId, Guid fieldId, string valueJson, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>Assigns a workspace member to the triggering item.</summary>
    Task AssignMemberAsync(Guid itemId, Guid userId, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>What a rule condition reads about the item.</summary>
    Task<WorkActionItemFacts> GetItemFactsAsync(Guid itemId, Guid boardId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>The option belongs to the board's Status field (NotFound otherwise).</summary>
    Task EnsureStatusOptionAsync(Guid boardId, Guid optionId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>The field is a writable field of the board and the value fits it.</summary>
    Task EnsureFieldValueAsync(Guid boardId, Guid fieldId, string valueJson, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>The user is an active member of the principal's workspace (Forbidden otherwise).</summary>
    Task EnsureWorkspaceMemberAsync(Guid userId, AutomationPrincipal principal, CancellationToken cancellationToken);

    /// <summary>The triggering item is on the rule's board (NotFound otherwise).</summary>
    Task EnsureItemOnBoardAsync(
        Guid itemId,
        Guid boardId,
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

/// <summary>The group, status option, name and raw JSON field values of an item, for rule conditions.</summary>
public sealed record WorkActionItemFacts(
    Guid GroupId,
    Guid? StatusOptionId,
    string Name,
    IReadOnlyDictionary<Guid, string> FieldValues);
