using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.Commands;

namespace Notrelix.Application.Features.Automation.CrossContext.WorkManagement;

/// <summary>
/// Automation-owned pure ACL: translates the Automation move-item semantic
/// (execution identity + automation principal) into the WorkManagement Public
/// move request. Pure translation only — no persistence, transport, provider
/// SDK, or dispatch dependencies.
/// </summary>
public static class WorkActionAcl
{
    public static WorkItemMoveRequest ToWorkMoveRequest(
        Guid itemId,
        Guid targetGroupId,
        Guid executionId,
        AutomationPrincipal automationPrincipal) =>
        new(
            new WorkItemActionIdentity(
                OperationId: executionId,
                WorkspaceId: automationPrincipal.WorkspaceId,
                ExecutorUserId: automationPrincipal.ExecutorUserId),
            ItemId: itemId,
            NewGroupId: targetGroupId);
}
