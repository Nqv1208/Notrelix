using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// The two timers of a call (SPEC-CHAT-MEDIA-CALLS invariant 5 and section 8): a call still Ringing after the ring time ends as Missed,
/// and an Active call that has had nobody in it for the grace period ends as Completed. The worker lists the due calls, then applies
/// each in its own transaction; a call that changed in between (answered, someone joined) is skipped, so a pass never ends a live call.
/// Each ending writes the one call message, like the commands do.
/// </summary>
public sealed class ChatCallSweeper(IChatDbContext db, ChatCallEnding ending)
{
    /// <summary>The ids of the calls that are due at <paramref name="now"/>.</summary>
    public Task<List<Guid>> ListDueAsync(DateTimeOffset now, TimeSpan ring, TimeSpan emptyGrace, CancellationToken cancellationToken)
    {
        var ringCutoff = now - ring;
        var emptyCutoff = now - emptyGrace;
        return db.ChatCalls.AsNoTracking()
            .Where(c => (c.State == CallState.Ringing && c.StartedAt <= ringCutoff)
                        || (c.State == CallState.Active && c.EmptySince != null && c.EmptySince <= emptyCutoff))
            .OrderBy(c => c.StartedAt)
            .Select(c => c.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Ends one due call; false when it is no longer due. The caller saves in the same transaction.</summary>
    public async Task<bool> ApplyAsync(Guid callId, DateTimeOffset now, TimeSpan ring, TimeSpan emptyGrace, CancellationToken cancellationToken)
    {
        var call = await db.ChatCalls.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);
        if (call is null || call.IsEnded) return false;

        if (call.State == CallState.Ringing && call.StartedAt <= now - ring)
            call.Miss(now);
        else if (call.State == CallState.Active && call.JoinedCount == 0 && call.EmptySince is { } since && since <= now - emptyGrace)
            call.EndEmpty(now);
        else
            return false;

        var space = await db.ChatSpaces.FirstOrDefaultAsync(s => s.Id == call.SpaceId && s.DeletedAt == null, cancellationToken);
        if (space is not null)
            await ending.FinishAsync(space, call, call.InitiatorId, cancellationToken);
        return true;
    }
}
