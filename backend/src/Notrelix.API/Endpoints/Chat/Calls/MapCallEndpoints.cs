using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Models;
using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Calls.Commands.AnswerCall;
using Notrelix.Application.Features.Chat.Calls.Commands.CancelCall;
using Notrelix.Application.Features.Chat.Calls.Commands.DeclineCall;
using Notrelix.Application.Features.Chat.Calls.Commands.EndCall;
using Notrelix.Application.Features.Chat.Calls.Commands.JoinCall;
using Notrelix.Application.Features.Chat.Calls.Commands.LeaveCall;
using Notrelix.Application.Features.Chat.Calls.Commands.StartCall;
using Notrelix.Application.Features.Chat.Calls.Commands.UpdateCallMediaState;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Queries.GetActiveCall;
using Notrelix.Application.Features.Chat.Calls.Queries.GetCallJoinToken;
using Notrelix.Application.Features.Chat.Calls.Queries.ListCalls;

namespace Notrelix.API.Endpoints.Chat.Calls;

/// <summary>
/// Voice and video calls of a space (SPEC-CHAT-MEDIA-CALLS section 6): start, the active call, answer, decline, cancel, end, join,
/// leave, the caller's media state and the call history. Media never goes through these endpoints; a join token is the way in.
/// </summary>
public static class MapCallEndpoints
{
    private const string VersionNote = "A stale expectedVersion is 412. A call that has ended is 409 call-ended. A person who is not an active member of the space is 404.";

    public static IEndpointRouteBuilder MapCallRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Calls")
            .WithOpenApi();

        group.MapWorkspacePost("/spaces/{spaceId:guid}/calls", StartAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Calls.Start")
            .WithSummary("Start a voice or video call in a space")
            .WithDescription("Returns the call and the starter's short-lived join token with 201. The stored idempotent answer holds no token: a retry with the same key returns the call with a freshly minted token, or is refused when the caller is no longer in the call. kind is voice or video. Idempotency-Key is required: a retry returns the stored answer. A person who is not an active member of the space, or a direct call to someone who blocked the caller or was blocked by them, is 404. A Guest or a member the channel keeps from posting is 403. The space already has a ringing or active call: 409 call-already-active. The caller is already in another call: 409 call-busy. When the other person of a direct call is in a call, the call is stored as ended with reason busy, no ring is sent and joinToken is null. An archived channel is 409 channel-archived. The media plane cannot be reached, or calls are off: 503 chat.calls.unavailable and nothing is stored.")
            .Produces<CallJoinDto>(StatusCodes.Status201Created)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status503ServiceUnavailable);

        group.MapWorkspaceGet("/spaces/{spaceId:guid}/calls/active", GetActiveAsync)
            .WithName("Chat.Calls.GetActive")
            .WithSummary("Get the ringing or active call of a space")
            .WithDescription("For the banner and re-join. 204 when the space has no live call. A space the caller is not an active member of is 404.")
            .Produces<CallDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspaceGet("/spaces/{spaceId:guid}/calls", ListAsync)
            .WithName("Chat.Calls.List")
            .WithSummary("List the call history of a space")
            .WithDescription("Newest call first. cursor is the nextCursor of the previous page; limit is 1 to 100, default 50. A space the caller is not an active member of is 404.")
            .Produces<CallPageDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/calls/{callId:guid}/answer", AnswerAsync)
            .WithName("Chat.Calls.Answer")
            .WithSummary("Answer a ringing call")
            .WithDescription("Ringing becomes active and the caller receives a join token. A repeat by someone already in the call changes nothing and returns the token again. A caller already in another call is 409 call-busy; a full call is 422 call-full. " + VersionNote)
            .Produces<CallJoinDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity, StatusCodes.Status503ServiceUnavailable);

        group.MapWorkspacePost("/calls/{callId:guid}/decline", DeclineAsync)
            .WithName("Chat.Calls.Decline")
            .WithSummary("Decline a ringing call")
            .WithDescription("A direct call ends as declined; in a group or channel call the call keeps ringing for the others and nothing changes. " + VersionNote)
            .Produces<CallDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePost("/calls/{callId:guid}/cancel", CancelAsync)
            .WithName("Chat.Calls.Cancel")
            .WithSummary("Cancel a ringing call (the starter)")
            .WithDescription("The call ends as cancelled. Anyone but the starter is 403 call-not-participant. " + VersionNote)
            .Produces<CallDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePost("/calls/{callId:guid}/end", EndAsync)
            .WithName("Chat.Calls.End")
            .WithSummary("End an active call for everyone")
            .WithDescription("In a direct call either person ends it; in a group or channel call only the starter does. A ringing call is cancelled or declined instead (409 call-state-invalid). " + VersionNote)
            .Produces<CallDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePost("/calls/{callId:guid}/join", JoinAsync)
            .WithName("Chat.Calls.Join")
            .WithSummary("Join a live group or channel call")
            .WithDescription("Returns a join token. A caller already in another call is 409 call-busy; a full call (25 voice, 12 video) is 422 call-full. A repeat changes nothing and returns the token again. " + VersionNote)
            .Produces<CallJoinDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity, StatusCodes.Status503ServiceUnavailable);

        group.MapWorkspacePost("/calls/{callId:guid}/leave", LeaveAsync)
            .WithName("Chat.Calls.Leave")
            .WithSummary("Leave an active call")
            .WithDescription("Leaving a direct call ends it; leaving a group or channel call keeps it alive, and the worker ends it after 60 seconds with nobody in it. A person who is not in the call changes nothing. " + VersionNote)
            .Produces<CallDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePut("/calls/{callId:guid}/participants/me", UpdateMediaStateAsync)
            .WithName("Chat.Calls.UpdateMediaState")
            .WithSummary("Set the caller's microphone, camera and screen share")
            .WithDescription("A voice call carries no camera (400 call-media-not-allowed). A caller who is not in the call is 403 call-not-participant. The same values change nothing. " + VersionNote)
            .Produces<CallDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> StartAsync(Guid workspaceId, Guid spaceId, StartCallRequest body, ISender sender, IServiceScopeFactory scopes, ICurrentRequestContext caller)
    {
        var started = await sender.Send(new StartCallCommand(workspaceId, spaceId, body.Kind));
        if (!started.Succeeded)
            return started.ToApiResult();

        // The start is stored for idempotent replay and carries no media token; the starter's token is minted fresh here, for a first
        // start and for every replay, after the caller's access to the call is checked again.
        // A request resolves its execution context once, so the token query cannot ride the same scope. It runs in a scope of its own and
        // goes through the whole pipeline (authentication, the same permission as Start, validation, tenant scope), seeded with the
        // caller exactly as the realtime gateway seeds its scope.
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;
        services.GetRequiredService<IExecutionContextAccessor>().SetUser(caller.UserId, caller.Email, caller.Name);
        services.GetRequiredService<ICurrentTenantContext>().SetUser(caller.UserId);
        var joined = await services.GetRequiredService<ISender>().Send(new GetCallJoinTokenQuery(workspaceId, started.Data!.Id));
        return joined.ToCreatedResult();
    }

    private static async Task<IResult> GetActiveAsync(Guid workspaceId, Guid spaceId, ISender sender)
    {
        var result = await sender.Send(new GetActiveCallQuery(workspaceId, spaceId));
        if (!result.Succeeded)
            return result.ToApiResult();
        return result.Data is null ? Results.NoContent() : Results.Ok(result.Data);
    }

    private static async Task<IResult> ListAsync(
        Guid workspaceId, Guid spaceId, [FromQuery] string? cursor, [FromQuery] int? limit, ISender sender)
    {
        var result = await sender.Send(new ListCallsQuery(workspaceId, spaceId, cursor, limit ?? ListCallsQuery.DefaultLimit));
        return result.ToApiResult();
    }

    private static async Task<IResult> AnswerAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new AnswerCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> DeclineAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new DeclineCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> CancelAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new CancelCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> EndAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new EndCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> JoinAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new JoinCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> LeaveAsync(Guid workspaceId, Guid callId, CallVersionRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new LeaveCallCommand(workspaceId, callId, v))).ToApiResult();

    private static async Task<IResult> UpdateMediaStateAsync(Guid workspaceId, Guid callId, UpdateCallMediaStateRequest body, ISender sender) =>
        body.ExpectedVersion is not { } v
            ? EndpointExtensions.InvalidInput("expectedVersion is required.")
            : (await sender.Send(new UpdateCallMediaStateCommand(workspaceId, callId, v, body.AudioOn, body.VideoOn, body.SharingScreen))).ToApiResult();
}
