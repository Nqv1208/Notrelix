using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// Access to a call ends with access to its space: when a person's space membership ends (removed, left, or gone with their Workspace
/// membership), their live participation in that space's call ends in the same transaction (<see cref="Call.Revoke"/>): the person is
/// released, a call left with nobody in it ends, and the call change reaches the realtime stream as any other. Our API refuses the person
/// from that commit on. The media room drops them through an outbox fact (<c>chat.call.participant-revoked</c>) whose consumer retries
/// until the media plane accepts it, so a failing media server never leaves the revocation undone and never fails open silently.
/// </summary>
public sealed class ChatCallRevocation(IChatDbContext db, ChatCallEnding ending)
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
        if (!call.Revoke(userId, at)) return false;

        if (call.IsEnded)
        {
            var space = await db.ChatSpaces.FirstOrDefaultAsync(s => s.Id == call.SpaceId && s.DeletedAt == null, cancellationToken);
            if (space is not null)
                await ending.FinishAsync(space, call, userId, cancellationToken);
        }

        return true;
    }
}
