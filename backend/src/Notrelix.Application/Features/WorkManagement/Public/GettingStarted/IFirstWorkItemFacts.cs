namespace Notrelix.Application.Features.WorkManagement.Public.GettingStarted;

/// <summary>
/// Work Management-published fact behind the Workspaces getting-started card: whether the people of a
/// Workspace have a first work Item. The caller passes the Board ids it already filtered with the
/// Governance collection visibility; the port never widens them.
/// </summary>
public interface IFirstWorkItemFacts
{
    /// <summary>
    /// The Board holding the earliest created active Item (not archived, not deleted) among the readable
    /// Boards, or null when there is no such Item.
    /// </summary>
    Task<FirstWorkItemBoard?> FindEarliestItemBoardAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readableBoardIds,
        CancellationToken cancellationToken);

    /// <summary>The most recently created readable active Board, or null when none exists.</summary>
    Task<FirstWorkItemBoard?> FindNewestBoardAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readableBoardIds,
        CancellationToken cancellationToken);
}

public sealed record FirstWorkItemBoard(Guid BoardId, string Title);
