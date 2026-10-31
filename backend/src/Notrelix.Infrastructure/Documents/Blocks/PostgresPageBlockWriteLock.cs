using System.Buffers.Binary;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Documents.Blocks;

/// <summary>
/// PostgreSQL implementation of <see cref="IPageBlockWriteLock"/>: <c>pg_advisory_xact_lock</c> on the
/// connection of the already-open request transaction, so the lock is released when that transaction commits
/// or rolls back. Different pages use different keys; a hash collision only over-serializes.
/// </summary>
public sealed class PostgresPageBlockWriteLock : IPageBlockWriteLock
{
    private readonly ApplicationDbContext _context;

    public PostgresPageBlockWriteLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(Guid pageId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The page block lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(pageId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>The first 8 bytes of SHA-256 of "documents.page-blocks:&lt;page-guid-N&gt;", read big-endian.</summary>
    public static long ComputeKey(Guid pageId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"documents.page-blocks:{pageId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
