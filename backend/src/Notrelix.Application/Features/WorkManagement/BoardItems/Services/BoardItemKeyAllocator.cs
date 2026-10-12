using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>One allocated Item key: the Board sequence number and its display key.</summary>
public readonly record struct BoardItemKey(long Sequence, string Key);

/// <summary>
/// Allocates Item keys from the Board in the creating request's transaction: it
/// takes the Board's key lock, loads the Board after the lock (so it reads the
/// committed sequence) and advances the sequence through the Board aggregate,
/// which bumps the Board version. Keys are therefore unique within the Board and
/// never reused. Callers take any ordering-scope lock first and allocate keys
/// last, so the key lock is always the innermost lock of a request.
/// </summary>
public sealed class BoardItemKeyAllocator
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardItemKeyLock _lock;

    public BoardItemKeyAllocator(IWorkManagementDbContext context, IBoardItemKeyLock keyLock)
    {
        _context = context;
        _lock = keyLock;
    }

    public async Task<IReadOnlyList<BoardItemKey>> AllocateAsync(
        Guid boardId, int count, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        await _lock.AcquireAsync(boardId, cancellationToken);

        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Board), boardId);

        var keys = new List<BoardItemKey>(count);
        for (var i = 0; i < count; i++)
        {
            var (sequence, key) = board.GenerateNextItemIdentity(actorUserId, now);
            keys.Add(new BoardItemKey(sequence, key));
        }

        return keys;
    }
}
