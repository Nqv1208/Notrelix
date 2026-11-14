using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.ReadState.Commands.MarkAllRead;
using Notrelix.Application.Features.Chat.ReadState.Commands.MarkSpaceRead;
using Notrelix.Application.Features.Chat.ReadState.Commands.SetNotificationLevel;
using Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;

namespace Notrelix.API.Endpoints.Chat.ReadState;

/// <summary>Read state of the caller: unread counts, mark read, mark all read, mute and notification level (UC-CHT-D01 to D07).</summary>
public static class MapReadStateEndpoints
{
    public static IEndpointRouteBuilder MapReadStateRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.ReadState")
            .WithOpenApi();

        group.MapWorkspaceGet("/me/unread", UnreadAsync)
            .WithName("Chat.ReadState.Unread")
            .WithSummary("Unread counts per space and in total")
            .WithDescription("One entry per space the caller is an active member of. unreadCount counts messages after the read boundary written by other people; deleted and system messages are not counted. totalUnread and totalMentions sum the spaces that are not muted. mentionCount is 0 until mentions are recorded.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/spaces/{spaceId:guid}/read", MarkReadAsync)
            .WithName("Chat.ReadState.MarkRead")
            .WithSummary("Move the read boundary of a space forward")
            .WithDescription("Idempotent. A sequence at or below the boundary changes nothing; a sequence above the space's last sequence is read to the end. Returns the boundary after the call. A space the caller is not a member of is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/me/read-all", MarkAllReadAsync)
            .WithName("Chat.ReadState.MarkAllRead")
            .WithSummary("Mark every space of the Workspace as read")
            .WithDescription("Moves each boundary the caller holds to the space's last sequence. Messages sent during the call stay unread. Returns the unread summary after the change.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/spaces/{spaceId:guid}/notification-level", SetNotificationLevelAsync)
            .WithName("Chat.ReadState.SetNotificationLevel")
            .WithSummary("Set how the caller is notified in a space, or mute it until a time")
            .WithDescription("level is all, mentions or none. none with an optional mutedUntil is a timed mute; a mutedUntil that is not in the future changes nothing. An until time with another level is 400. Last write wins. A space the caller is not a member of is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UnreadAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetUnreadSummaryQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> MarkReadAsync(Guid workspaceId, Guid spaceId, MarkSpaceReadRequest body, ISender sender)
    {
        if (body.UpToSequence is not { } upToSequence)
            return EndpointExtensions.InvalidInput("upToSequence is required.");

        var result = await sender.Send(new MarkSpaceReadCommand(workspaceId, spaceId, upToSequence));
        return result.ToApiResult();
    }

    private static async Task<IResult> MarkAllReadAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new MarkAllReadCommand(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> SetNotificationLevelAsync(Guid workspaceId, Guid spaceId, SetNotificationLevelRequest body, ISender sender)
    {
        var result = await sender.Send(new SetNotificationLevelCommand(workspaceId, spaceId, body.Level, body.MutedUntil));
        return result.ToApiResult();
    }
}
