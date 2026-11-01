using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemRepeat;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class SetBoardItemRepeatEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardItemRepeat(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/repeat", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.SetRepeat")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Set or clear how a board item repeats");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        SetBoardItemRepeatRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardItemRepeatCommand(itemId, body.Rule, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
