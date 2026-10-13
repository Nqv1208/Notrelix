using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>One allocated Item key: the Board sequence number and its display key.</summary>
public readonly record struct BoardItemKey(long Sequence, string Key);

/// <summary>
/// Reserves Item keys from a Board's item sequence inside the active request
/// transaction. The reservation is one row-locking statement on the Board row, so
/// concurrent creates are serialized by the database and never draw the same
/// number; it does not touch the Board aggregate's version or audit stamp, so a
/// client holding the Board version is unaffected by Item creation.
/// </summary>
public interface IBoardItemKeyAllocator
{
    /// <summary>
    /// Reserves <paramref name="count"/> consecutive numbers and returns their keys
    /// in order, or null when no active (not deleted, not archived) Board with that
    /// id is visible.
    /// </summary>
    Task<IReadOnlyList<BoardItemKey>?> TryAllocateAsync(Guid boardId, int count, CancellationToken cancellationToken);
}

public static class BoardItemKeyAllocation
{
    /// <summary>
    /// Allocates keys for Items being created on the Board. An archived Board
    /// rejects Item creation; a missing or deleted Board is not found.
    /// </summary>
    public static async Task<IReadOnlyList<BoardItemKey>> AllocateAsync(
        this IBoardItemKeyAllocator allocator,
        IWorkManagementDbContext context,
        Guid boardId,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        var keys = await allocator.TryAllocateAsync(boardId, count, cancellationToken);
        if (keys is not null) return keys;

        if (await context.Boards.AsNoTracking().AnyAsync(board => board.Id == boardId && board.IsArchived, cancellationToken))
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived,
                "Cannot create an item on an archived board.");
        }

        throw new NotFoundException(nameof(Board), boardId);
    }
}
