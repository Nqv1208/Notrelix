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

    private static WorkItemActionIdentity Identity(Guid operationId, AutomationPrincipal principal) =>
        new(
            OperationId: operationId,
            AccountId: principal.AccountId,
            WorkspaceId: principal.WorkspaceId,
            ExecutorUserId: principal.ExecutorUserId,
            CorrelationId: principal.CorrelationId ?? operationId,
            CausationId: principal.CausationId);

    public Task ChangeStatusAsync(
        Guid itemId, Guid optionId, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.ChangeStatusAsync(Identity(operationId, principal), itemId, optionId, cancellationToken);

    public Task SetFieldValueAsync(
        Guid itemId, Guid fieldId, string valueJson, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.SetFieldValueAsync(Identity(operationId, principal), itemId, fieldId, valueJson, cancellationToken);

    public Task AssignMemberAsync(
        Guid itemId, Guid userId, Guid operationId, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.AssignMemberAsync(Identity(operationId, principal), itemId, userId, cancellationToken);

    public async Task<WorkActionItemFacts> GetItemFactsAsync(
        Guid itemId, Guid boardId, AutomationPrincipal principal, CancellationToken cancellationToken)
    {
        var facts = await _workItemActions.GetItemFactsAsync(principal.WorkspaceId, itemId, boardId, cancellationToken);
        return new WorkActionItemFacts(facts.GroupId, facts.StatusOptionId, facts.Name, facts.FieldValues);
    }

    public Task EnsureStatusOptionAsync(
        Guid boardId, Guid optionId, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.EnsureStatusOptionAsync(principal.WorkspaceId, boardId, optionId, cancellationToken);

    public Task EnsureFieldValueAsync(
        Guid boardId, Guid fieldId, string valueJson, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.EnsureFieldValueAsync(principal.WorkspaceId, boardId, fieldId, valueJson, cancellationToken);

    public Task EnsureWorkspaceMemberAsync(
        Guid userId, AutomationPrincipal principal, CancellationToken cancellationToken) =>
        _workItemActions.EnsureWorkspaceMemberAsync(principal.WorkspaceId, userId, cancellationToken);
}
