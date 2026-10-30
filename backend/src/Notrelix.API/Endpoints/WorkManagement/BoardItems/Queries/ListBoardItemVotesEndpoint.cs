using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemVotes;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemVotesEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItemVotes(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/votes/{fieldId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListVotes")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List who voted on a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid fieldId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardItemVotesQuery(itemId, fieldId), cancellationToken);
        return result.ToApiResult();
    }
}
