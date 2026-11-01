using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RecordBoardItemView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class RecordBoardItemViewEndpoint
{
    public static IEndpointRouteBuilder MapRecordBoardItemView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/views", HandleAsync)
            .WithName("WorkManagement.BoardItems.RecordView")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Record that the caller opened a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RecordBoardItemViewCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
