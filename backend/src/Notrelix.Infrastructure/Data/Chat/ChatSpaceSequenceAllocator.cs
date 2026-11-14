using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Infrastructure.Data.Chat;

/// <summary>
/// The per-space sequence in Postgres (CHT-005). <see cref="LockAsync"/> takes the space row lock with SELECT ... FOR
/// UPDATE; <see cref="NextAsync"/> advances <c>chat.spaces.last_sequence</c> with UPDATE ... RETURNING. Both run on the
/// request's transaction, so the lock holds until the message is stored or the request fails.
/// </summary>
public sealed class ChatSpaceSequenceAllocator(ApplicationDbContext db) : ISpaceSequenceAllocator
{
    public async Task LockAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var found = await db.Database
            .SqlQuery<long>($"SELECT last_sequence AS value FROM chat.spaces WHERE id = {spaceId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        if (found.Count == 0)
            throw new NotFoundException("Space", spaceId);
    }

    public async Task<long> NextAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var next = await db.Database
            .SqlQuery<long>($"UPDATE chat.spaces SET last_sequence = last_sequence + 1 WHERE id = {spaceId} RETURNING last_sequence AS value")
            .ToListAsync(cancellationToken);
        if (next.Count == 0)
            throw new NotFoundException("Space", spaceId);
        return next[0];
    }
}
