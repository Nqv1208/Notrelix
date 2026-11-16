using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// Access to a call ends with access to its space: when a person's space membership ends (removed, left, or gone with their Workspace
/// membership), their live participation in that space's call ends in the same transaction. The person is released (<c>Leave</c>, which
/// ends a direct call), a call left with nobody in it ends, the room drops them at once through the media gateway, and the call change
/// reaches the realtime stream as any other. Revocation is instant: nothing waits for the worker, which only sweeps what a missed event left.
/// </summary>
public sealed class ChatCallRevocation(
    IChatDbContext db,
    IChatCallMediaGateway media,
    ChatCallEnding ending,
    ILogger<ChatCallRevocation> logger)
{
    /// <returns>True when a live participation of the person ended.</returns>
    public async Task<bool> ReleaseAsync(Guid workspaceId, Guid spaceId, Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var call = await db.ChatCalls.Include(c => c.Participants).FirstOrDefaultAsync(
            c => c.SpaceId == spaceId && c.WorkspaceId == workspaceId && c.State != CallState.Ended, cancellationToken);
        return call is not null && await ReleaseAsync(call, userId, at, cancellationToken);
    }

    /// <summary>Releases the person from this call (already loaded with its participants).</summary>
    public async Task<bool> ReleaseAsync(Call call, Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        if (call.IsEnded || !call.IsJoined(userId)) return false;

        try
        {
            await media.RemoveParticipantAsync(call.RoomKey, userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Removing a person from the media room of call {CallId} failed; the room closes when the call ends", call.Id);
        }

        if (call.State == CallState.Ringing)
            call.Cancel(userId, at); // only the starter is in a ringing call
        else
        {
            call.Leave(userId, at);
            if (!call.IsEnded && call.JoinedCount == 0)
                call.EndEmpty(at);
        }

        if (call.IsEnded)
        {
            var space = await db.ChatSpaces.FirstOrDefaultAsync(s => s.Id == call.SpaceId && s.DeletedAt == null, cancellationToken);
            if (space is not null)
                await ending.FinishAsync(space, call, userId, cancellationToken);
        }

        return true;
    }
}
