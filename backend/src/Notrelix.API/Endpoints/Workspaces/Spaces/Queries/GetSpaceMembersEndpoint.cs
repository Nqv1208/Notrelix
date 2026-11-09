using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
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
            .WithDescription(
                "Cursor-paginated (default 50, max 200) in subject id order. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<SpaceMemberDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceMembersQuery(workspaceId, spaceId, cursor, limit));
        return result.ToApiResult();
    }
}
