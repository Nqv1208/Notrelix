using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListChildItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListChildItemsEndpoint
{
    public static IEndpointRouteBuilder MapListChildItems(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/child-items", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListChildren")
            .Produces<ChildItemListDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List the sub-items of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListChildItemsQuery(itemId), cancellationToken);
        return Results.Ok(result);
    }
}
