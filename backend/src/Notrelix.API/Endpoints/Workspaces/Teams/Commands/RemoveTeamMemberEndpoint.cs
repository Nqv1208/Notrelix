using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class RemoveTeamMemberEndpoint
{
    public static IEndpointRouteBuilder MapRemoveTeamMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/{userId:guid}", HandleAsync)
            .WithName("Workspaces.Teams.RemoveTeamMember")
            .WithTags("Workspaces.Teams")
            .WithSummary("Remove a member from a team")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        Guid userId,
        ISender sender,
        [FromQuery] long expectedVersion)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(workspaceId, teamId, userId, expectedVersion));
        return result.ToNoContentResult();
    }
}
