using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemVote;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class SetBoardItemVoteEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardItemVote(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/votes/{fieldId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.SetVote")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Cast or withdraw the caller's vote on a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid fieldId,
        SetBoardItemVoteRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardItemVoteCommand(itemId, fieldId, body.Voted), cancellationToken);
        return result.ToApiResult();
    }
}
