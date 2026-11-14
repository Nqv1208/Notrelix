using System.Buffers.Binary;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>
/// PostgreSQL implementation of <see cref="IReactionWriteLock"/>. It takes <c>pg_advisory_xact_lock</c> on the
/// connection of the already-open request transaction, so the lock is released exactly when that transaction
/// commits or rolls back. The key is derived from the comment, the person and the emoji, so only concurrent
/// adds of the same reaction serialize; a hash collision only over-serializes.
/// </summary>
public sealed class PostgresReactionWriteLock : IReactionWriteLock
{
    private readonly ApplicationDbContext _context;

    public PostgresReactionWriteLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(Guid commentId, Guid userId, string emojiCode, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The reaction write lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(commentId, userId, emojiCode);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>
    /// SHA-256 of UTF-8 "collab.reactions:&lt;comment-guid-N&gt;:&lt;user-guid-N&gt;:&lt;emoji&gt;"; the first 8 bytes,
    /// read big-endian, are the signed 64-bit advisory lock key.
    /// </summary>
    public static long ComputeKey(Guid commentId, Guid userId, string emojiCode)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"collab.reactions:{commentId:N}:{userId:N}:{emojiCode}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
