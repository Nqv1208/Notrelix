using System.Buffers.Binary;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// PostgreSQL implementation of <see cref="IBoardItemKeyLock"/>: a
/// <c>pg_advisory_xact_lock</c> on the connection of the already-open request
/// transaction, released exactly when that transaction commits or rolls back.
/// The key is derived like the ordering-scope keys, from a distinct scope name,
/// so it never serializes against an ordering scope except by hash collision.
/// </summary>
public sealed class PostgresBoardItemKeyLock : IBoardItemKeyLock
{
    private readonly ApplicationDbContext _context;

    public PostgresBoardItemKeyLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(Guid boardId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The board item-key lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(boardId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>SHA-256 of UTF-8 "board-item-keys:&lt;board-guid-N&gt;"; first 8 bytes big-endian.</summary>
    public static long ComputeKey(Guid boardId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"board-item-keys:{boardId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
