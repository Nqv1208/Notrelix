using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class InviteMemberEndpoint
{
    public static IEndpointRouteBuilder MapInviteMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Workspaces.Members.InviteMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Invite a member to workspace")
            .WithDescription("Needs an Idempotency-Key: a retry with the same key returns the first invitation and sends no second email. A pending invitation for the same address answers 409 with the stable code workspaces.invitation.pending-exists.")
            .Produces<Guid>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        InviteMemberRequest body,
        ISender sender)
    {
        var result = await sender.Send(new InviteMemberCommand(
            workspaceId,
            body.Email,
            Enum.Parse<WorkspaceRole>(body.Role, ignoreCase: true),
            body.InitialEntries?.Select(e => new InvitationInitialEntryInput(e.Kind, e.TargetId, e.Role)).ToList()));
        return result.ToCreatedResult();
    }
}
