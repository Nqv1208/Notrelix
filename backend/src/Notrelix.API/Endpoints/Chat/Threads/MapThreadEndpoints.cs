using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Threads.Commands.FollowThread;
using Notrelix.Application.Features.Chat.Threads.Commands.MarkThreadRead;
using Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;
using Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;
using Notrelix.Application.Features.Chat.Threads.Queries.ListThreadReplies;

namespace Notrelix.API.Endpoints.Chat.Threads;

/// <summary>Threads: reply, read replies, follow or mute, mark read, and the threads I follow (UC-CHT-F01, F03, F04, F08).</summary>
public static class MapThreadEndpoints
{
    public static IEndpointRouteBuilder MapThreadRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Threads")
            .WithOpenApi();

        group.MapWorkspacePost("/messages/{rootId:guid}/replies", ReplyAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Threads.Reply")
            .WithSummary("Reply in the thread of a message")
            .WithDescription("clientMessageId is the sender's key: a retry with the same one returns the stored reply with 200 and stores nothing; the first reply returns 201. alsoSendToChannel also shows the reply in the channel's main flow. The reply takes the next thread sequence. Replying reopens an archived thread and makes the replier and the root's author followers. An archived channel is 409 channel-archived; a reply to a reply or a system message is 409 thread-root-invalid; a deleted root with no replies is 409 message-deleted. A space the caller is not a member of is 404. mentions and confirmBroadcast work as in a send. A new reply is counted per person and per space (chat.thread-reply), a broadcast in it under chat.broadcast, and it waits for the channel's slow mode: over a limit the answer is 429 with Retry-After and ruleCode rate-limited or slow-mode.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status429TooManyRequests);

        group.MapWorkspaceGet("/messages/{rootId:guid}/replies", ListRepliesAsync)
            .WithName("Chat.Threads.ListReplies")
            .WithSummary("Read a thread: its root, its summary and a page of replies")
            .WithDescription("Replies ascend by thread sequence. after is the thread sequence of the last reply already held; limit is 1 to 100, default 50. Deleted replies are tombstones. A space the caller cannot see is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/threads/{threadId:guid}/follow", FollowAsync)
            .WithName("Chat.Threads.Follow")
            .WithSummary("Follow, mute or unfollow a thread")
            .WithDescription("following true follows the thread; muted true with it follows without notifications; following false removes the follow. A new follow starts with the replies already posted read. The thread must exist and the caller must be a member of its space; otherwise 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/threads/{threadId:guid}/read", MarkReadAsync)
            .WithName("Chat.Threads.MarkRead")
            .WithSummary("Move the caller's read pointer in a followed thread forward")
            .WithDescription("upToReplySequence is the thread sequence the person has read. A lower value changes nothing; a higher one is read to the end. Returns the unread replies left. A thread the caller does not follow keeps no pointer and returns 0 unread. Otherwise 404 when the thread is missing or not visible.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspaceGet("/me/threads", ListFollowedAsync)
            .WithName("Chat.Threads.Mine")
            .WithSummary("List the threads I follow, with unread reply counts")
            .WithDescription("Most recent reply first. Only threads of spaces the caller is still an active member of are listed. cursor is the nextCursor of the previous page; limit is 1 to 100, default 50.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> ReplyAsync(Guid workspaceId, Guid rootId, ReplyInThreadRequest body, ISender sender)
    {
        var result = await sender.Send(new ReplyInThreadCommand(
            workspaceId, rootId, body.Content ?? string.Empty, body.ClientMessageId ?? string.Empty, body.AlsoSendToChannel ?? false,
            body.Mentions?.Select(m => m.ToInput()).ToList(), body.ConfirmBroadcast ?? false));
        if (!result.Succeeded)
            return result.ToApiResult();

        return result.Data!.Created
            ? Results.Json(result.Data.Message, statusCode: StatusCodes.Status201Created)
            : Results.Ok(result.Data.Message);
    }

    private static async Task<IResult> ListRepliesAsync(
        Guid workspaceId,
        Guid rootId,
        [FromQuery] long? after,
        [FromQuery] int? limit,
        ISender sender)
    {
        var result = await sender.Send(new ListThreadRepliesQuery(
            workspaceId, rootId, after, limit ?? ListThreadRepliesQuery.DefaultLimit));
        return result.ToApiResult();
    }

    private static async Task<IResult> FollowAsync(Guid workspaceId, Guid threadId, FollowThreadRequest body, ISender sender)
    {
        if (body.Following is not { } following)
            return EndpointExtensions.InvalidInput("following is required.");

        var result = await sender.Send(new FollowThreadCommand(workspaceId, threadId, following, body.Muted ?? false));
        return result.ToApiResult();
    }

    private static async Task<IResult> MarkReadAsync(Guid workspaceId, Guid threadId, MarkThreadReadRequest body, ISender sender)
    {
        if (body.UpToReplySequence is not { } upTo)
            return EndpointExtensions.InvalidInput("upToReplySequence is required.");

        var result = await sender.Send(new MarkThreadReadCommand(workspaceId, threadId, upTo));
        return result.ToApiResult();
    }

    private static async Task<IResult> ListFollowedAsync(Guid workspaceId, [FromQuery] string? cursor, [FromQuery] int? limit, ISender sender)
    {
        var result = await sender.Send(new ListFollowedThreadsQuery(
            workspaceId, cursor, limit ?? ListFollowedThreadsQuery.DefaultLimit));
        return result.ToApiResult();
    }
}
