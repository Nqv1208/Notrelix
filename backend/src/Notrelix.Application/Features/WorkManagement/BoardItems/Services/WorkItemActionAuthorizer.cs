using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-owned canonical authorization for the WorkManagement public target
/// action. Locates the board item authoritatively, then evaluates the exact
/// same request contract the HTTP pipeline evaluates (MoveItem on
/// work-management.board-item) through the one canonical facts provider and
/// policy engine — no second evaluator, no governance raw query.
/// </summary>
public interface IWorkItemActionAuthorizer
{
    Task AuthorizeMoveItemAsync(
        WorkItemActionIdentity execution,
        Guid itemId,
        Guid newGroupId,
        CancellationToken cancellationToken);
}

public sealed class WorkItemActionAuthorizer : IWorkItemActionAuthorizer
{
    private static readonly ResourceKind BoardItemKind =
        ResourceKind.Create("work-management.board-item");

    private readonly IResourceLocator _resourceLocator;
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;

    public WorkItemActionAuthorizer(
        IResourceLocator resourceLocator,
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy)
    {
        _resourceLocator = resourceLocator;
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
    }

    public async Task AuthorizeMoveItemAsync(
        WorkItemActionIdentity execution,
        Guid itemId,
        Guid newGroupId,
        CancellationToken cancellationToken)
    {
        var resource = ResourceRef.Create(BoardItemKind, itemId, execution.WorkspaceId);

        // Authoritative resource location: the canonical facts query resolves
        // workspace-scoped existence through the located tenant, so the action
        // must never claim a resource it did not locate.
        var location = await _resourceLocator.LocateAsync(resource, execution.ExecutorUserId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", itemId);

        if (location.AccountId != execution.AccountId
            || location.WorkspaceId != execution.WorkspaceId)
        {
            throw new ForbiddenException("The item does not belong to the declared execution scope.");
        }

        // The public action is governed by the same canonical decision as the
        // HTTP command: MoveItem over the board-item resource.
        var descriptor = _descriptors.GetRequired(typeof(MoveBoardItemCommand));
        var request = new MoveBoardItemCommand(itemId, newGroupId, Position: 0d);
        var snapshot = new ExecutionContextSnapshot(
            execution.ExecutorUserId,
            location.AccountId,
            location.WorkspaceId,
            resource,
            ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Resource,
            (execution.CorrelationId ?? execution.OperationId).ToString("D"));

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        var decision = _policy.Evaluate(descriptor, snapshot, facts, request);
        if (decision.Kind != AccessDecisionKind.Allowed)
        {
            throw new ForbiddenException(decision.Message ?? "You do not have permission to perform this action.");
        }
    }
}