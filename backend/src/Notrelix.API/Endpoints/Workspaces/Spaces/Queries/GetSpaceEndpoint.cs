using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetSpaceEndpoint
{
    public static IEndpointRouteBuilder MapGetSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Spaces.GetSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get a space by ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceQuery(workspaceId, spaceId));
        return result.ToApiResult();
    }
}
