using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class AddLabelToBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapAddLabelToBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapPost("/labels", HandleAsync)
            .WithName("WorkManagement.BoardItems.AddLabel")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Add a label to board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        AddLabelToBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddLabelToBoardItemCommand(itemId, body.LabelId), cancellationToken);
        return result.ToNoContentResult();
    }
}

