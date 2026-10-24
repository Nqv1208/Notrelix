namespace Notrelix.Application.Features.Documents.Public.SpaceOverview;

/// <summary>
/// Documents-published facts of the root Pages of a Space for the Workspaces Space overview
/// (CTR-GAP-WS-SPACE-OVERVIEW). The caller passes the Page ids it already filtered with the Governance
/// collection visibility; only active root Pages (no parent, not archived, not deleted) placed in the Space count.
/// </summary>
public interface IPageSpaceOverviewFacts
{
    Task<SpaceRootPages> ReadRootPagesAsync(
        Guid workspaceId,
        Guid spaceId,
        IReadOnlyCollection<Guid> readablePageIds,
        int take,
        CancellationToken cancellationToken);
}

/// <param name="Total">Readable root Pages of the Space.</param>
/// <param name="Recent">The most recently changed, newest first.</param>
public sealed record SpaceRootPages(int Total, IReadOnlyList<SpaceRootPage> Recent);

/// <param name="EditorUserId">Who made the last change (the creator when never edited).</param>
public sealed record SpaceRootPage(Guid Id, string Title, Guid? EditorUserId, DateTimeOffset UpdatedAt);
