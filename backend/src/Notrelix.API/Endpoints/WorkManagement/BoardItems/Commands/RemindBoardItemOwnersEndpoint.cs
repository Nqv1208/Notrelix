using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RemindBoardItemOwners;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class RemindBoardItemOwnersEndpoint
{
    public static IEndpointRouteBuilder MapRemindBoardItemOwners(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/remind", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.RemindOwners")
            .Produces<RemindBoardItemOwnersResult>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Remind the people in charge of an item that the caller is waiting on it")
            .WithDescription(
                "Sends each assignee except the caller an Inbox notification naming the caller. At most once per " +
                "item per caller per 24 hours: a later request answers 409 (work.item.remind-too-soon) and says when " +
                "it is possible again. Seeing the item is enough; nobody else in charge answers 400.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemindBoardItemOwnersCommand(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
