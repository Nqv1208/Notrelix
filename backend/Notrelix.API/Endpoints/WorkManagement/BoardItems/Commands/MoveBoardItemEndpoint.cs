using MediatR;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveCard;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class MoveBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapMoveBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapPost("/move", HandleAsync)
            .WithName("WorkManagement.BoardItems.Move")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Move board item to another group or change position");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        MoveBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveCardCommand(itemId, body.GroupId, body.Position), cancellationToken);
        return result.ToApiResult();
    }
}

