using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaceMembership;

namespace Notrelix.API.Endpoints.Workspaces.Members.Queries;

public static class GetMyMembershipEndpoint
{
    public static IEndpointRouteBuilder MapGetMyMembership(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/me", HandleAsync)
            .WithName("Workspaces.Members.GetMyMembership")
            .WithTags("Workspaces.Members")
            .WithSummary("Get your own membership in this workspace")
            .WithDescription("Any member of the workspace, a Guest included, reads exactly their own row (role, status, job title, version). It never returns other people; the member list is closed to a Guest.")
            .Produces<WorkspaceMemberDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetMyWorkspaceMembershipQuery(workspaceId));
        return result.ToApiResult();
    }
}
