using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.Commands;

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
}
