namespace Notrelix.Application.Features.Documents.Public.PageSearch;

/// <summary>
/// Documents-published search over Pages for the Workspaces command search (ADR-010). The caller passes the Page
/// ids it already filtered with the Governance collection visibility; this port never decides who may read a Page
/// and never widens the set. Only active Pages (not archived, not deleted) are searched, by title, mark- and
/// case-insensitively. A Page's ancestor path names only ancestors that are in the readable set, so a title the
/// caller cannot read is never disclosed through it. Page text is not searched yet.
/// </summary>
public interface IPageSearchFacts
{
    Task<IReadOnlyList<PageSearchHit>> SearchAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readablePageIds,
        string text,
        int limit,
        CancellationToken cancellationToken);
}

/// <param name="SpaceId">The Space the Page is placed in; the caller decides whether it may be disclosed.</param>
/// <param name="Ancestors">Titles of the readable ancestors, outermost first, at most six, stopping at the first unreadable one.</param>
/// <param name="EditorUserId">Who made the last change (the creator when never edited).</param>
public sealed record PageSearchHit(
    Guid Id,
    string Title,
    Guid? SpaceId,
    IReadOnlyList<string> Ancestors,
    DateTimeOffset UpdatedAt,
    Guid? EditorUserId);
