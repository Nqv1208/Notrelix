using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;

namespace Notrelix.API.Endpoints.Workspaces.Navigation.Queries;

public static class GetRecentResourcesEndpoint
{
    public static IEndpointRouteBuilder MapGetRecentResources(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Navigation.ListRecents")
            .WithTags("Workspaces.Navigation")
            .WithSummary("List the current member's recently opened Boards and Docs")
            .WithDescription("Newest first, at most 30 entries. References only; display names come from the Board and Doc lists.")
            .Produces<IReadOnlyList<RecentResourceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRecentResourcesQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
