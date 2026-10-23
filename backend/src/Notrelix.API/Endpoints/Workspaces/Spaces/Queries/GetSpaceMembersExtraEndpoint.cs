using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembersExtra;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetSpaceMembersExtraEndpoint
{
    public static IEndpointRouteBuilder MapGetSpaceMembersExtra(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/members-extra", HandleAsync)
            .WithName("Workspaces.Spaces.GetSpaceMembersExtra")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get what the department members tab adds to the space member list")
            .WithDescription("People reached by the space's user and team entries once each at their highest role, with job title, the teams that reach them and who made the space, plus pending invitations that name the space. Email and activity are not exposed yet and board guests are listed through board sharing. A space the caller cannot see is 404.")
            .Produces<SpaceMembersExtraDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceMembersExtraQuery(workspaceId, spaceId));
        return result.ToApiResult();
    }
}
