using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UpdateBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.Update")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Update board item properties");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        UpdateBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardItemCommand(
            itemId, body.Title, body.DueDate, body.StartDate, body.ExpectedVersion), cancellationToken);
        return result.ToApiResult();
    }
}
