using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Domain.Chat.Calls;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// What every terminal state of a call does besides the state change: writes the one call system message in the space (invariant 6)
/// and closes the media room. The room is closed after the change is stored in the same transaction; a failing media plane is logged
/// and the reconciliation job closes the room later, so a call never stays open because the media server was unreachable.
/// </summary>
public sealed class ChatCallEnding(
    ChatSystemMessages systemMessages,
    IChatCallMediaGateway media,
    ILogger<ChatCallEnding> logger)
{
    public async Task FinishAsync(Space space, Call call, Guid actorId, CancellationToken cancellationToken)
    {
        if (!call.IsEnded) return;

        await systemMessages.AnnounceCallAsync(space, actorId, call.Id, Describe(call), cancellationToken);
        try
        {
            await media.EndRoomAsync(call.RoomKey, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Closing the media room of call {CallId} in space {SpaceId} failed; reconciliation closes it", call.Id, call.SpaceId);
        }
    }

    /// <summary>The text of the call message: the kind, how it ended and, for a completed call, how long it lasted.</summary>
    public static string Describe(Call call)
    {
        var kind = call.Kind == CallKind.Video ? "video call" : "voice call";
        return call.EndReason switch
        {
            CallEndReason.Completed => $"{kind} ended, lasted {Duration(call)}",
            CallEndReason.Missed => $"missed {kind}",
            CallEndReason.Declined => $"{kind} declined",
            CallEndReason.Cancelled => $"{kind} cancelled",
            CallEndReason.Busy => $"{kind} not answered, the other person is in a call",
            CallEndReason.Failed => $"{kind} failed",
            _ => kind,
        };
    }

    private static string Duration(Call call)
    {
        var span = (call.EndedAt ?? call.StartedAt) - (call.AnsweredAt ?? call.StartedAt);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes}m"
            : span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds}s" : $"{Math.Max(0, (int)span.TotalSeconds)}s";
    }
}
