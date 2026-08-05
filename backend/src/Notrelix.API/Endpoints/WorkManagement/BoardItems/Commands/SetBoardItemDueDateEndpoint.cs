using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class SetBoardItemDueDateEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardItemDueDate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/due-date", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.SetDueDate")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Set due date and start date for a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        SetBoardItemDueDateRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardItemDueDateCommand(itemId, body.DueDate, body.StartDate), cancellationToken);
        return result.ToNoContentResult();
    }
}
