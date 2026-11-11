using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Pins.Commands.PinMessage;
using Notrelix.Application.Features.Chat.Pins.Commands.UnpinMessage;
using Notrelix.Application.Features.Chat.Pins.Queries.ListPins;

namespace Notrelix.API.Endpoints.Chat.Pins;

/// <summary>Pins: pin a message, unpin it, and list a space's pins (UC-CHT-F06, F07).</summary>
public static class MapPinEndpoints
{
    public static IEndpointRouteBuilder MapPinRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Pins")
            .WithOpenApi();

        group.MapWorkspacePut("/messages/{messageId:guid}/pin", PinAsync)
            .WithName("Chat.Pins.Pin")
            .WithSummary("Pin a message in its channel")
            .WithDescription("Idempotent: a message already pinned returns its pin with 200 and changes nothing; a new pin returns 201 and announces itself in the channel with a system message. A space holds at most 50 pins; the next one is 422 limit-reached. A system or deleted message is 409 (message-not-pinnable or message-deleted). An archived channel is 409 channel-archived. A message the caller cannot see, or a space the caller is not a member of, is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity);

        group.MapWorkspaceDelete("/messages/{messageId:guid}/pin", UnpinAsync)
            .WithName("Chat.Pins.Unpin")
            .WithSummary("Unpin a message")
            .WithDescription("Idempotent: a message that is not pinned returns 204 and changes nothing. Pins also end when the message is deleted. An archived channel is 409 channel-archived; a message the caller cannot see is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        group.MapWorkspaceGet("/spaces/{spaceId:guid}/pins", ListAsync)
            .WithName("Chat.Pins.List")
            .WithSummary("List the pinned messages of a channel")
            .WithDescription("Newest pin first. cursor is the nextCursor of the previous page; limit is 1 to 100, default 50. A space the caller cannot see is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> PinAsync(Guid workspaceId, Guid messageId, ISender sender)
    {
        var result = await sender.Send(new PinMessageCommand(workspaceId, messageId));
        if (!result.Succeeded)
            return result.ToApiResult();

        return result.Data!.Created
            ? Results.Json(result.Data.Pin, statusCode: StatusCodes.Status201Created)
            : Results.Ok(result.Data.Pin);
    }

    private static async Task<IResult> UnpinAsync(Guid workspaceId, Guid messageId, ISender sender)
    {
        var result = await sender.Send(new UnpinMessageCommand(workspaceId, messageId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ListAsync(
        Guid workspaceId,
        Guid spaceId,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        ISender sender)
    {
        var result = await sender.Send(new ListPinsQuery(
            workspaceId, spaceId, cursor, limit ?? ListPinsQuery.DefaultLimit));
        return result.ToApiResult();
    }
}
