using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetSpaceMembersEndpoint
{
    public static IEndpointRouteBuilder MapGetSpaceMembers(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/members", HandleAsync)
            .WithName("Workspaces.Spaces.GetSpaceMembers")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get the user and team entries of a space")
            .Produces<List<SpaceMemberDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceMembersQuery(workspaceId, spaceId));
        return result.ToApiResult();
    }
}
