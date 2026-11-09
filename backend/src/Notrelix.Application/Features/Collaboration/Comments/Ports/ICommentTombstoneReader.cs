using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Collaboration.Comments.Ports;

/// <summary>
/// Reads the deleted comments of one target that still have live replies, so the thread can keep them as tombstones.
/// Deleted comments are hidden from every other read, which is why this read exists. The caller has already been
/// authorized to view the target. Oldest first, after the cursor, at most <paramref name="limit"/> rows (all when null).
/// </summary>
public interface ICommentTombstoneReader
{
    Task<IReadOnlyList<Comment>> ListAsync(
        ResourceKind targetKind,
        Guid targetId,
        TimeCursorPosition? after,
        int? limit,
        CancellationToken cancellationToken);
}
