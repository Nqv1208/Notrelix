namespace Notrelix.Application.Features.WorkManagement.Public.BoardTitles;

/// <summary>
/// Work Management-published display fact: the titles of live Boards (not deleted) of a Workspace, so Chat can name the Board a channel
/// is linked to (UC-CHT-H10, CHT-023). It decides no visibility: the caller filters the ids by what the person may view (the Governance
/// collection visibility) and never discloses a title for an id the person cannot see. Chat never reads Work persistence itself.
/// </summary>
public interface IBoardTitleFacts
{
    Task<IReadOnlyDictionary<Guid, string>> TitlesAsync(Guid workspaceId, IReadOnlyCollection<Guid> boardIds, CancellationToken cancellationToken);
}
