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
        UpdateBoardItemCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var cmd = command with { BoardItemId = itemId };
        var result = await sender.Send(cmd, cancellationToken);
        return result.ToApiResult();
    }
}
