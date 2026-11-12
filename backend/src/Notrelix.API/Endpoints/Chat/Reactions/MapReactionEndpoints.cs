using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Commands.RemoveReaction;
using Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;

namespace Notrelix.API.Endpoints.Chat.Reactions;

/// <summary>Reactions: react to a message, remove the reaction, and list who reacted (UC-CHT-G06, G07).</summary>
public static class MapReactionEndpoints
{
    public static IEndpointRouteBuilder MapReactionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Reactions")
            .WithOpenApi();

        group.MapWorkspacePut("/messages/{messageId:guid}/reactions/{kind}", AddAsync)
            .WithName("Chat.Reactions.Add")
            .WithSummary("React to a message with a palette emoji")
            .WithDescription("Idempotent: a reaction the caller already has returns the message's reaction summary with 200 and changes nothing; a new one returns 201. The kind is one of the six palette emoji; any other kind is 400. A system or deleted message is 409 (message-not-reactable or message-deleted). An archived channel is 409 channel-archived. A message the caller cannot see, or a space the caller is not a member of, is 404. A new reaction is counted per person and per space (chat.react): over the limit the answer is 429 with Retry-After and ruleCode rate-limited.")
            .Produces<MessageReactionsDto>(StatusCodes.Status200OK)
            .Produces<MessageReactionsDto>(StatusCodes.Status201Created)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity, StatusCodes.Status429TooManyRequests);

        group.MapWorkspaceDelete("/messages/{messageId:guid}/reactions/{kind}", RemoveAsync)
            .WithName("Chat.Reactions.Remove")
            .WithSummary("Remove the caller's reaction from a message")
            .WithDescription("Idempotent: a reaction the caller does not have returns the message's reaction summary with 200 and changes nothing. The kind is one of the six palette emoji; any other kind is 400. An archived channel is 409 channel-archived; a message the caller cannot see is 404.")
            .Produces<MessageReactionsDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        group.MapWorkspaceGet("/messages/{messageId:guid}/reactions", ListReactorsAsync)
            .WithName("Chat.Reactions.ListReactors")
            .WithSummary("List who reacted to a message")
            .WithDescription("Earliest reaction first. kind narrows the list to one emoji; cursor is the nextCursor of the previous page; limit is 1 to 100, default 50. A message the caller cannot see, or a private channel the caller is not a member of, is 404.")
            .Produces<ReactorPageDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> AddAsync(Guid workspaceId, Guid messageId, string kind, ISender sender)
    {
        var result = await sender.Send(new AddReactionCommand(workspaceId, messageId, kind));
        if (!result.Succeeded)
            return result.ToApiResult();

        return result.Data!.Created
            ? Results.Json(result.Data.Message, statusCode: StatusCodes.Status201Created)
            : Results.Ok(result.Data.Message);
    }

    private static async Task<IResult> RemoveAsync(Guid workspaceId, Guid messageId, string kind, ISender sender)
    {
        var result = await sender.Send(new RemoveReactionCommand(workspaceId, messageId, kind));
        return result.ToApiResult();
    }

    private static async Task<IResult> ListReactorsAsync(
        Guid workspaceId,
        Guid messageId,
        [FromQuery] string? kind,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        ISender sender)
    {
        var result = await sender.Send(new ListReactorsQuery(
            workspaceId, messageId, kind, cursor, limit ?? ListReactorsQuery.DefaultLimit));
        return result.ToApiResult();
    }
}
