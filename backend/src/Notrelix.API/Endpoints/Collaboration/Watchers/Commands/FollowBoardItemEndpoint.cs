using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

namespace Notrelix.API.Endpoints.Collaboration.Watchers.Commands;

public static class FollowBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapFollowBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Collaboration.Watchers.FollowBoardItem")
            .WithTags("Collaboration.Watchers")
            .WithSummary("Follow a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(new FollowBoardItemCommand(boardItemId));
        return result.ToNoContentResult();
    }
}
