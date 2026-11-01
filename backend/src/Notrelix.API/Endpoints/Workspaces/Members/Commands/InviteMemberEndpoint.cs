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
            .WithName("Workspaces.Members.InviteMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Invite a member to workspace")
            .WithDescription("A person who is already an Active member of the workspace answers 409 with the code workspaces.invitation.already-member; no invitation is created and no email is sent. Someone removed or suspended may be invited again.")
            .Produces<Guid>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(StatusCodes.Status409Conflict);
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
