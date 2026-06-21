using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class RemoveLabelFromBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapRemoveLabelFromBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/labels/{labelId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardItems.RemoveLabel")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Remove a label from board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid labelId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveLabelFromCardCommand(itemId, labelId), cancellationToken);
        return result.ToNoContentResult();
    }
}
