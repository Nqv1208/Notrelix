using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Search.Queries.CommandSearch;

namespace Notrelix.API.Endpoints.Workspaces.Search.Queries;

public static class CommandSearchEndpoint
{
    public static IEndpointRouteBuilder MapCommandSearch(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Search.CommandSearch")
            .WithTags("Workspaces.Search")
            .WithSummary("Search Items, Docs and Boards the caller may open")
            .WithDescription("Items by name or key (or by a sub-item's name) across the Boards the caller can read, Docs by title, and Boards by name or by holding at least two matching Items. Ignores case and marks (\"hoa don\" finds \"Hoá đơn\"); text shorter than two characters answers empty. At most 6 Items (the caller's own first), 5 Docs and 5 Boards. Only what the caller can open is returned or counted. Doc text is not searched yet.")
            .Produces<CommandSearchDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? q,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CommandSearchQuery(workspaceId, q), cancellationToken);
        return result.ToApiResult();
    }
}
