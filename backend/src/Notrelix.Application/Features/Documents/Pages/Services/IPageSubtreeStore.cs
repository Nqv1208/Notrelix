namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Documents persistence port for page subtrees: a root page's move to another
/// Space re-places every descendant (a nested page follows its root page).
/// Returned pages are tracked and saved by the request data session.
/// </summary>
public interface IPageSubtreeStore
{
    Task<Page?> FindAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>Every non-deleted descendant (children, grandchildren, …) of the page.</summary>
    Task<IReadOnlyList<Page>> LoadDescendantsAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);

    /// <summary>
    /// The page's level in the tree (a root page is 1), walking up at most one level past
    /// <see cref="Notrelix.Domain.Documents.Rules.PageTreeRules.MaxDepth"/>: a deeper answer is only "too deep".
    /// </summary>
    Task<int> DepthOfAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken);
}
