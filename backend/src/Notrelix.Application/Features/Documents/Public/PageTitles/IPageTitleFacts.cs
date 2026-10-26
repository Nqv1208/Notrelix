namespace Notrelix.Application.Features.Documents.Public.PageTitles;

/// <summary>
/// Documents-published display fact: the titles of live Pages of a Workspace. Work Management's inbox uses it
/// to name the page a mention is about; it never reads Documents persistence itself and decides no visibility
/// (the caller filters the ids by what the member may read).
/// </summary>
public interface IPageTitleFacts
{
    Task<IReadOnlyDictionary<Guid, string>> TitlesAsync(Guid workspaceId, IReadOnlyCollection<Guid> pageIds, CancellationToken cancellationToken);
}
