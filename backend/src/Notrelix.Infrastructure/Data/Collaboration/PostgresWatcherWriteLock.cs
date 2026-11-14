using System.Buffers.Binary;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>
/// PostgreSQL implementation of <see cref="IWatcherWriteLock"/>. It takes <c>pg_advisory_xact_lock</c> on the
/// connection of the already-open request transaction, so the lock is released exactly when that transaction
/// commits or rolls back. The key is derived from the person and the target, so only concurrent follows of the
/// same target by the same person serialize; a hash collision only over-serializes.
/// </summary>
public sealed class PostgresWatcherWriteLock : IWatcherWriteLock
{
    private readonly ApplicationDbContext _context;

    public PostgresWatcherWriteLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(Guid userId, ResourceRef target, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The watcher write lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(userId, target);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>
    /// SHA-256 of UTF-8 "collab.watchers:&lt;user-guid-N&gt;:&lt;resource-kind&gt;:&lt;resource-guid-N&gt;"; the first 8 bytes,
    /// read big-endian, are the signed 64-bit advisory lock key.
    /// </summary>
    public static long ComputeKey(Guid userId, ResourceRef target)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"collab.watchers:{userId:N}:{target.Kind.Value}:{target.ResourceId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
