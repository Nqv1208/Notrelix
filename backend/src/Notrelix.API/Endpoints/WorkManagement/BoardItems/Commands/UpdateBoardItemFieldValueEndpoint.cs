using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UpdateBoardItemFieldValueEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardItemFieldValue(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/values/{fieldId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.UpdateFieldValue")
            .Produces<BoardItemSlimDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Update cell value of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid fieldId,
        UpdateBoardItemFieldValueRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardItemFieldValueCommand(
            itemId,
            fieldId,
            body.Value,
            body.ExpectedVersion), cancellationToken);
        return Results.Ok(result);
    }
}

