using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.EditMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;

namespace Notrelix.API.Endpoints.Chat.Messages;

/// <summary>Messages of a space: send, read by cursor, edit and delete (UC-CHT-B01 to B05).</summary>
public static class MapMessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Messages")
            .WithOpenApi();

        group.MapWorkspaceGet("/spaces/{spaceId:guid}/messages", ListAsync)
            .WithName("Chat.Messages.List")
            .WithSummary("List a page of a space's messages")
            .WithDescription("Give at most one of before, after and around (sequence numbers). With none, the newest page. Ascending by sequence; tombstones are included. limit is 1 to 100, default 50. A space the caller cannot see is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/spaces/{spaceId:guid}/messages", SendAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Messages.Send")
            .WithSummary("Send a message to a space")
            .WithDescription("clientMessageId is the sender's key for the message: a retry with the same one returns the stored message with 200 and stores nothing. The first send returns 201. Idempotency-Key is the transport key and is required on this endpoint. An archived channel is 409 channel-archived; a space the caller is not a member of is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        group.MapWorkspacePatch("/messages/{messageId:guid}", EditAsync)
            .WithName("Chat.Messages.Edit")
            .WithSummary("Edit the text of your own message")
            .WithDescription("Only the author edits. A stale expectedVersion is 412. A deleted message or a system message cannot be edited (409). The sequence never changes.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspaceDelete("/messages/{messageId:guid}", DeleteAsync)
            .WithName("Chat.Messages.Delete")
            .WithSummary("Delete your own message, leaving a tombstone")
            .WithDescription("The content is removed; the author, the time and the sequence stay. Pass expectedVersion as a query parameter; 412 when it is stale. Deleting an existing tombstone changes nothing.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid workspaceId,
        Guid spaceId,
        [FromQuery] long? before,
        [FromQuery] long? after,
        [FromQuery] long? around,
        [FromQuery] int? limit,
        ISender sender)
    {
        var result = await sender.Send(new ListMessagesQuery(
            workspaceId, spaceId, before, after, around, limit ?? ListMessagesQuery.DefaultLimit));
        return result.ToApiResult();
    }

    private static async Task<IResult> SendAsync(Guid workspaceId, Guid spaceId, SendMessageRequest body, ISender sender)
    {
        var result = await sender.Send(new SendMessageCommand(
            workspaceId, spaceId, body.Content ?? string.Empty, body.ClientMessageId ?? string.Empty, body.ReplyToId));
        if (!result.Succeeded)
            return result.ToApiResult();

        return result.Data!.Created
            ? Results.Json(result.Data.Message, statusCode: StatusCodes.Status201Created)
            : Results.Ok(result.Data.Message);
    }

    private static async Task<IResult> EditAsync(Guid workspaceId, Guid messageId, EditMessageRequest body, ISender sender)
    {
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");

        var result = await sender.Send(new EditMessageCommand(workspaceId, messageId, body.Content ?? string.Empty, expectedVersion));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid messageId, [FromQuery] long? expectedVersion, ISender sender)
    {
        if (expectedVersion is not { } version)
            return EndpointExtensions.InvalidInput("expectedVersion is required as a query parameter.");

        var result = await sender.Send(new DeleteMessageCommand(workspaceId, messageId, version));
        return result.ToNoContentResult();
    }
}
