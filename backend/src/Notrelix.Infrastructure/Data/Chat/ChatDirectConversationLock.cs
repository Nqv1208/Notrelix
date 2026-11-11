using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Infrastructure.Data.Chat;

/// <summary>
/// The per-pair lock of a direct conversation start (UC-CHT-E01) as a Postgres transaction-level advisory lock on a hash of
/// (Workspace, low, high). It is held until the request's transaction commits or rolls back, so a second start of the same pair
/// waits and then reads the conversation the first one stored. Starts of other pairs are not blocked by it (a hash collision only
/// makes two unrelated starts wait for each other, never wrong).
/// </summary>
public sealed class ChatDirectConversationLock(ApplicationDbContext db) : IDirectConversationLock
{
    public async Task LockPairAsync(Guid workspaceId, Guid userLow, Guid userHigh, CancellationToken cancellationToken)
    {
        var key = $"chat.direct:{workspaceId:D}:{userLow:D}:{userHigh:D}";
        await db.Database
            .SqlQuery<long>($"SELECT count(*) AS value FROM (SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))) AS locked")
            .ToListAsync(cancellationToken);
    }
}
