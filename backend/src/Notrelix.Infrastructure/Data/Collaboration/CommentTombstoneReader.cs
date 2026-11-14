using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Ports;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>
/// Reads the soft-deleted top-level comments of one target that still have a live reply. The query filter hides every
/// deleted row, so this one lifts it for that narrow set. The target predicate bounds the rows to one page or board item
/// the caller already may view, and row-level security bounds them to the caller's account.
/// </summary>
public sealed class CommentTombstoneReader : ICommentTombstoneReader
{
    private readonly ICollaborationDbContext _context;

    public CommentTombstoneReader(ICollaborationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Comment>> ListAsync(
        ResourceKind targetKind,
        Guid targetId,
        TimeCursorPosition? after,
        int? limit,
        CancellationToken cancellationToken)
    {
        // A reply is one level deep, so only a deleted top-level comment can be a tombstone. The live check is explicit
        // because the lifted filter also applies inside the subquery.
        var parentsOfLiveReplies = _context.Comments.AsNoTracking()
            .Where(r => r.DeletedAt == null && r.ParentId != null && r.Target.Kind == targetKind && r.Target.ResourceId == targetId)
            .Select(r => r.ParentId!.Value);

        var tombstones = _context.Comments.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && c.ParentId == null && c.Target.Kind == targetKind && c.Target.ResourceId == targetId)
            .Where(c => parentsOfLiveReplies.Contains(c.Id));

        if (after is { } position)
        {
            tombstones = tombstones.Where(c => c.CreatedAt > position.At || (c.CreatedAt == position.At && c.Id.CompareTo(position.Id) > 0));
        }

        var ordered = tombstones.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        return await (limit is { } take ? ordered.Take(take) : ordered).ToListAsync(cancellationToken);
    }
}
