using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetWorkspaceSpacesEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceSpaces(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Spaces.GetWorkspaceSpaces")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get the spaces of a workspace the caller can see")
            .WithDescription(
                "Cursor-paginated (default 50, max 200), ordered by name. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<SpaceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceSpacesQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
