using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;

namespace Notrelix.API.Endpoints.Workspaces.Navigation.Queries;

public static class GetPinnedResourcesEndpoint
{
    public static IEndpointRouteBuilder MapGetPinnedResources(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Navigation.ListPins")
            .WithTags("Workspaces.Navigation")
            .WithSummary("List the current member's pinned Boards and Docs")
            .WithDescription("Ordered by pin time, newest first. References only; display names come from the Board and Doc lists.")
            .Produces<IReadOnlyList<PinnedResourceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPinnedResourcesQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
