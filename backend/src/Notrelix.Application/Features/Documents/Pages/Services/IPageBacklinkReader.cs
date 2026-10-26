namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Reads which pages mention a page. Backlinks are derived, never stored: a
/// page mentions another page through a page-reference block (its content
/// names the target as <c>linkedPageId</c>) or a page mention in a text block
/// (<c>pageMentionIds</c>).
/// </summary>
public interface IPageBacklinkReader
{
    /// <summary>
    /// Ids of the other pages of the Workspace with a live block that mentions
    /// <paramref name="targetPageId"/>. Visibility of those pages is the
    /// caller's concern.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindMentioningPageIdsAsync(Guid workspaceId, Guid targetPageId, CancellationToken cancellationToken);
}
