using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.UnfollowBoardItem;

namespace Notrelix.API.Endpoints.Collaboration.Watchers.Commands;

public static class UnfollowBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUnfollowBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Collaboration.Watchers.UnfollowBoardItem")
            .WithTags("Collaboration.Watchers")
            .WithSummary("Stop following a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(new UnfollowBoardItemCommand(boardItemId));
        return result.ToNoContentResult();
    }
}
