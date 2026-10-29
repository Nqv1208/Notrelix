using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemPageLinks;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemPageLinksEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItemPageLinks(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/linked-pages", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListLinkedPages")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List the Documents pages linked to a board item that the caller may open")
            .WithDescription("Pages the caller cannot see are not named; they are counted in hiddenCount.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardItemPageLinksQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
