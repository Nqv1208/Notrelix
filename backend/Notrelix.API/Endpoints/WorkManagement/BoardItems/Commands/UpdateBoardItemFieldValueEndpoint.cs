using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UpdateBoardItemFieldValueEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardItemFieldValue(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/values/{fieldId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardItems.UpdateFieldValue")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Update cell value of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        [FromHeader(Name = "X-Board-Id")] Guid boardId,
        UpdateBoardItemFieldValueRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardItemFieldValueCommand(
            workspaceId,
            boardId,
            itemId,
            fieldId,
            body.Value), cancellationToken);
        return Results.Ok(result);
    }
}

