using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class InviteMemberEndpoint
{
    public static IEndpointRouteBuilder MapInviteMember(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("Workspaces.Members.InviteMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Invite a member to workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        InviteMemberRequest body,
        ISender sender)
    {
        var result = await sender.Send(new InviteMemberCommand(workspaceId, body.Email, Enum.Parse<WorkspaceRole>(body.Role, ignoreCase: true)));
        return result.ToCreatedResult();
    }
}
