namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>
/// What a step-up proof is bound to besides user, session and purpose. A handover proof names the
/// workspace and the incoming Owner, so a proof given for one person cannot hand the workspace to another.
/// </summary>
public static class StepUpTarget
{
    public static string OwnershipTransfer(Guid workspaceId, Guid newOwnerId) =>
        $"workspace:{workspaceId:N}:owner:{newOwnerId:N}";
}
