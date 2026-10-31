namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Documents persistence port for page subtrees: a root page's move to another
/// Space re-places every descendant (a nested page follows its root page).
/// Returned pages are tracked and saved by the request data session.
/// </summary>
public interface IPageSubtreeStore
{
    /// <summary>
    /// Serializes the writes that change the shape of the page tree of a Workspace (a move, a Space move, a
    /// create under a parent). Cycle, depth and Space checks read the tree and are only true until another such
    /// write commits, so a write takes this lock before its first read of the tree and a second one reads what
    /// the first committed. Held until the request's transaction ends.
    /// </summary>
    Task LockTreeAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<Page?> FindAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>Every non-deleted descendant (children, grandchildren, …) of the page.</summary>
    Task<IReadOnlyList<Page>> LoadDescendantsAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>
    /// Levels from the page down to its deepest descendant, the page itself counting as 1. Reads only the
    /// shape (ids and parents), nothing is tracked; trashed and deleted pages keep their place in the tree and count, like the deleted ancestors the ancestry walk passes through.
    /// </summary>
    Task<int> SubtreeDepthAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>
    /// The page's level in the tree (a root page is 1), deleted ancestors counting, walking up at most one level past
    /// <see cref="Notrelix.Domain.Documents.Rules.PageTreeRules.MaxDepth"/>: a deeper answer is only "too deep".
    /// </summary>
    Task<int> DepthOfAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>
    /// The page and every ancestor above it, deleted ones included, as id to parent id (a root maps to null): the ancestry facts a move
    /// hands the Domain for its cycle check, and the page's level is the number of entries. Not tracked, and
    /// bounded so damaged data (a cycle) ends the walk.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid?>> LoadAncestryAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);
}
