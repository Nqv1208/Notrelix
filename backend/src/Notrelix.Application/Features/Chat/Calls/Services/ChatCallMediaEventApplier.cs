using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// Applies one verified media server event to the call that owns the room (SPEC-CHAT-MEDIA-CALLS section 6): the person left the room, so
/// they leave the call; the room finished, so the call ends; someone connected who holds no open participation, so they are dropped. The
/// media plane only repeats what the call rules already allow, so every case goes through the aggregate's own method and a state the
/// call is not in (already ended, already left, still ringing) changes nothing. The caller owns the transaction and the save.
/// </summary>
public sealed class ChatCallMediaEventApplier(
    IChatDbContext db,
    ChatCallEnding ending,
    IChatCallMediaGateway media,
    ILogger<ChatCallMediaEventApplier> logger)
{
    public const string ParticipantJoined = "participant_joined";
    public const string ParticipantLeft = "participant_left";
    public const string RoomFinished = "room_finished";

    /// <returns>True when the call changed.</returns>
    public async Task<bool> ApplyAsync(CallMediaWebhookEvent mediaEvent, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (mediaEvent.Kind is not (ParticipantJoined or ParticipantLeft or RoomFinished)) return false;

        var call = await db.ChatCalls.Include(c => c.Participants).FirstOrDefaultAsync(c => c.RoomKey == mediaEvent.RoomKey, cancellationToken);
        if (call is null) return false;

        var changed = false;
        switch (mediaEvent.Kind)
        {
            case ParticipantJoined when mediaEvent.PersonId is { } joined && (call.IsEnded || !call.IsJoined(joined)):
                await DropAsync(call, joined, cancellationToken);
                return false;

            case ParticipantLeft when mediaEvent.PersonId is { } left && !call.IsEnded && call.State == CallState.Active:
                changed = call.Leave(left, now);
                break;

            case RoomFinished:
                changed = call.EndFromMedia(now);
                break;
        }

        if (!changed || !call.IsEnded) return changed;

        var space = await db.ChatSpaces.FirstOrDefaultAsync(s => s.Id == call.SpaceId && s.DeletedAt == null, cancellationToken);
        if (space is not null)
            await ending.FinishAsync(space, call, call.InitiatorId, cancellationToken);
        return true;
    }

    private async Task DropAsync(Call call, Guid personId, CancellationToken cancellationToken)
    {
        try
        {
            await media.RemoveParticipantAsync(call.RoomKey, personId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not drop a person who joined the media room of call {CallId} without an open participation; reconciliation retries", call.Id);
        }
    }
}
