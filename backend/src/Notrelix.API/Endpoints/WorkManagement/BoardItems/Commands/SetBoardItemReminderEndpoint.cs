using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemReminder;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class SetBoardItemReminderEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardItemReminder(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/reminder", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.SetReminder")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Set how long before the due time a board item reminds");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        SetBoardItemReminderRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardItemReminderCommand(itemId, body.OffsetMinutes, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
