using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

/// <summary>
/// Reads what a proof is for. A handover proof must name the workspace and the incoming Owner (it is then
/// refused for any other); a purpose that acts on no single thing takes no target, whatever is sent.
/// </summary>
internal static class StepUpTargetInput
{
    public const string Missing = "workspaceId and newOwnerId are required for TransferWorkspaceOwnership.";

    public static bool TryResolve(StepUpPurpose purpose, Guid? workspaceId, Guid? newOwnerId, out string? target)
    {
        target = null;
        if (purpose != StepUpPurpose.TransferWorkspaceOwnership)
            return true;

        if (workspaceId is not { } workspace || workspace == default || newOwnerId is not { } owner || owner == default)
            return false;

        target = StepUpTarget.OwnershipTransfer(workspace, owner);
        return true;
    }
}
