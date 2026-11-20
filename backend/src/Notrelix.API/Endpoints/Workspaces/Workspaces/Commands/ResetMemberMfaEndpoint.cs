using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ResetMemberMfa;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class ResetMemberMfaEndpoint
{
    public static IEndpointRouteBuilder MapResetMemberMfa(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/members/{memberId:guid}/reset-mfa", HandleAsync)
            .WithName("Workspaces.Workspaces.ResetMemberMfa")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Reset a member's second factor (Owner only)")
            .WithDescription("Only the workspace Owner may. Needs a single-use ResetMemberMfa step-up proof bound to this workspace and this member (412 identity.security.step-up-required without one; 400 identity.security.step-up-invalid when stale, spent or given for another member). Removes the member's factors and recovery codes, ends their sessions, records an activity line and mails them. An Owner's factors cannot be reset; a non-Owner caller gets 404.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid memberId,
        ResetMemberMfaRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ResetMemberMfaCommand(workspaceId, memberId, request.StepUpToken));
        return result.ToNoContentResult();
    }
}

public sealed record ResetMemberMfaRequest(string StepUpToken = "");
