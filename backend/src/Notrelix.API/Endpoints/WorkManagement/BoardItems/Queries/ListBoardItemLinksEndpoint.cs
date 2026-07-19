using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemLinks;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemLinksEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItemLinks(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/links", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListLinks")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List links for a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardItemLinksQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
