using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// PostgreSQL implementation of <see cref="IBoardItemKeyAllocator"/>: one
/// <c>UPDATE … RETURNING</c> that advances <c>boards.item_sequence</c> by the
/// requested count on the active Board's row. The row lock it takes is held until
/// the request transaction ends, so concurrent creates on one Board run one after
/// another and each reads the committed sequence. Only <c>item_sequence</c> is
/// written: the Board's version, audit stamp and events are untouched. Must run
/// inside the request transaction (the RLS session context applies to it).
/// </summary>
public sealed class PostgresBoardItemKeyAllocator : IBoardItemKeyAllocator
{
    private readonly ApplicationDbContext _context;

    public PostgresBoardItemKeyAllocator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BoardItemKey>?> TryAllocateAsync(Guid boardId, int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Item keys must be allocated inside the active request transaction.");
        }

        var reserved = await _context.Database.SqlQuery<ReservedRange>($"""
            UPDATE work.boards
               SET item_sequence = item_sequence + {count}
             WHERE id = {boardId}
               AND deleted_at IS NULL
               AND NOT is_archived
            RETURNING item_sequence AS last_sequence, item_key_prefix AS prefix
            """).ToListAsync(cancellationToken);

        if (reserved.Count == 0) return null;

        var range = reserved[0];
        var first = range.LastSequence - count + 1;
        return Enumerable.Range(0, count)
            .Select(offset => first + offset)
            .Select(sequence => new BoardItemKey(sequence, Board.FormatItemKey(range.Prefix, sequence)))
            .ToList();
    }

    private sealed record ReservedRange(long LastSequence, string? Prefix);
}
