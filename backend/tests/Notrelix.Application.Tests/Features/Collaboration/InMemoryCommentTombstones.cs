using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Comments.Ports;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// The tombstone read over an in-memory list: a deleted top-level comment of the target that a live reply still names,
/// oldest first, after the cursor, at most the limit. The Infrastructure read does the same against the database.
/// </summary>
internal sealed class InMemoryCommentTombstones(List<Comment> rows) : ICommentTombstoneReader
{
    public Task<IReadOnlyList<Comment>> ListAsync(
        ResourceKind targetKind,
        Guid targetId,
        TimeCursorPosition? after,
        int? limit,
        CancellationToken cancellationToken)
    {
        var liveReplyParents = rows
            .Where(r => r.DeletedAt == null && r.ParentId is not null && r.Target.Kind == targetKind && r.Target.ResourceId == targetId)
            .Select(r => r.ParentId!.Value)
            .ToHashSet();

        IEnumerable<Comment> found = rows.Where(c =>
            c.DeletedAt != null && c.ParentId == null && c.Target.Kind == targetKind && c.Target.ResourceId == targetId
            && liveReplyParents.Contains(c.Id));
        if (after is { } position)
        {
            found = found.Where(c => c.CreatedAt > position.At || (c.CreatedAt == position.At && c.Id.CompareTo(position.Id) > 0));
        }

        found = found.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        if (limit is { } take) found = found.Take(take);
        return Task.FromResult<IReadOnlyList<Comment>>(found.ToList());
    }
}
