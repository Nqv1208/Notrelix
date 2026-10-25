using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

namespace Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;

/// <summary>
/// Runtime adapter implementing the Automation Work-action port through the
/// WorkManagement Public target action. No Work DbContext, no Work Domain
/// aggregates, no business policy — replaceable without touching Automation
/// handlers.
/// </summary>
public sealed class WorkItemActionAdapter : IWorkActionPort
{
    private readonly IWorkItemActions _workItemActions;

    public WorkItemActionAdapter(IWorkItemActions workItemActions)
    {
        _workItemActions = workItemActions;
    }

    public async Task<WorkActionResult> MoveItemAsync(
        Guid itemId,
        Guid targetGroupId,
        Guid executionId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken)
    {
        var request = WorkActionAcl.ToWorkMoveRequest(itemId, targetGroupId, executionId, automationPrincipal);
        var result = await _workItemActions.MoveItemAsync(request, cancellationToken);

        return new WorkActionResult(result.ItemId, result.GroupId, result.Position);
    }

    public Task EnsureBoardGroupAsync(
        Guid boardId,
        Guid? groupId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken) =>
        _workItemActions.EnsureBoardGroupAsync(
            new WorkItemActionIdentity(
                OperationId: Guid.CreateVersion7(),
                AccountId: automationPrincipal.AccountId,
                WorkspaceId: automationPrincipal.WorkspaceId,
                ExecutorUserId: automationPrincipal.ExecutorUserId,
                CorrelationId: automationPrincipal.CorrelationId,
                CausationId: automationPrincipal.CausationId),
            boardId,
            groupId,
            cancellationToken);

    public Task EnsureItemOnBoardAsync(
        Guid itemId,
        Guid boardId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken) =>
        _workItemActions.EnsureItemOnBoardAsync(
            automationPrincipal.WorkspaceId, itemId, boardId, cancellationToken);

    public Task EnsureMoveItemAllowedAsync(
        Guid itemId,
        Guid targetGroupId,
        AutomationPrincipal automationPrincipal,
        CancellationToken cancellationToken)
    {
        // The probe has no execution of its own: a fresh operation id keeps the
        // identity well-formed and nothing is deduplicated or persisted.
        var request = WorkActionAcl.ToWorkMoveRequest(itemId, targetGroupId, Guid.CreateVersion7(), automationPrincipal);
        return _workItemActions.EnsureMoveItemAllowedAsync(request, cancellationToken);
    }
}
