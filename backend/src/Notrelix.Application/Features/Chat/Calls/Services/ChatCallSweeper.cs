using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// The two timers of a call (SPEC-CHAT-MEDIA-CALLS invariant 5 and section 8): a call still Ringing after the ring time ends as Missed,
/// and an Active call that has had nobody in it for the grace period ends as Completed. The worker lists the due calls, then applies
/// each in its own transaction; a call that changed in between (answered, someone joined) is skipped, so a pass never ends a live call.
/// Each ending writes the one call message, like the commands do.
/// </summary>
public sealed class ChatCallSweeper(
    IChatDbContext db,
    ChatCallEnding ending,
    ChatCallRevocation revocation,
    IChatCallMediaGateway media,
    ILogger<ChatCallSweeper> logger)
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

    /// <summary>
    /// The people still seated in a live call whose membership of its space has ended, which a missed event left behind. They are released
    /// before the timers run, so a call they alone kept open can end as empty.
    /// </summary>
    public async Task<List<(Guid CallId, Guid PersonId)>> ListRevokedAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from p in db.ChatCallParticipants.AsNoTracking()
            join c in db.ChatCalls.AsNoTracking() on p.CallId equals c.Id
            where p.LeftAt == null && c.State != CallState.Ended
                && !db.ChatMemberships.Any(m => m.SpaceId == c.SpaceId && m.UserId == p.PersonId && m.LeftAt == null)
            select new { c.Id, p.PersonId }).Take(500).ToListAsync(cancellationToken);
        return rows.Select(r => (r.Id, r.PersonId)).ToList();
    }

    /// <summary>Releases one such person; false when they are no longer seated. The caller saves in the same transaction.</summary>
    public async Task<bool> ApplyRevokedAsync(Guid callId, Guid personId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var call = await db.ChatCalls.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);
        return call is not null && await revocation.ReleaseAsync(call, personId, now, cancellationToken);
    }

    /// <summary>
    /// The safety net under the retried kick: for every live call, anyone the media room lists who has no open participation (left, or no
    /// row at all) is removed from the room. It reads the database and writes only to the media plane, and it does not depend on the token
    /// life. A failing room is logged and tried again on the next pass.
    /// </summary>
    /// <returns>The number of people removed from a room.</returns>
    public async Task<int> ReconcileRoomsAsync(CancellationToken cancellationToken)
    {
        var live = await db.ChatCalls.AsNoTracking().Include(c => c.Participants)
            .Where(c => c.State != CallState.Ended).Take(500).ToListAsync(cancellationToken);

        var removed = 0;
        foreach (var call in live)
        {
            try
            {
                var open = call.Participants.Where(p => p.IsJoined).Select(p => p.PersonId).ToHashSet();
                foreach (var person in (await media.ListParticipantsAsync(call.RoomKey, cancellationToken)).Where(p => !open.Contains(p)))
                {
                    try
                    {
                        await media.RemoveParticipantAsync(call.RoomKey, person, cancellationToken);
                        removed++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "Reconciliation could not remove a person from the media room of call {CallId}; it is tried again next pass", call.Id);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Reconciliation could not list the media room of call {CallId}; it is tried again next pass", call.Id);
            }
        }

        return removed;
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
