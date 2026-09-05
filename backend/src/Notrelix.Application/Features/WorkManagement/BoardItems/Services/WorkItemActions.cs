using Notrelix.Application.Features.WorkManagement.Public.Commands;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-owned implementation of the WorkManagement public item action.
/// Delegates to the single producer-local move use case shared with the HTTP
/// command handler — no duplicated domain mutation logic.
/// </summary>
public sealed class WorkItemActions : IWorkItemActions
{
    private readonly MoveBoardItemUseCase _useCase;

    public WorkItemActions(MoveBoardItemUseCase useCase)
    {
        _useCase = useCase;
    }

    public async Task<WorkItemMoveResult> MoveItemAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await _useCase.MoveAsync(
            request.ItemId,
            request.NewGroupId,
            request.Execution.ExecutorUserId,
            cancellationToken);

        return new WorkItemMoveResult(
            outcome.Item.Id,
            outcome.Item.GroupId,
            outcome.Item.Position);
    }
}
