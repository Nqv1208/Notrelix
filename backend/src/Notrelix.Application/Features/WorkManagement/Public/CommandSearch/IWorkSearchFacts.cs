namespace Notrelix.Application.Features.WorkManagement.Public.CommandSearch;

/// <summary>
/// Work Management-published search over Boards and Items for the Workspaces command search (ADR-010). The
/// caller passes the Board ids it already filtered with the Governance collection visibility (GOV-029); this port
/// never decides who may read a Board and never widens the set, and a row of a Board outside it is neither
/// returned nor counted. Only active Boards and Items (not archived, not deleted) are searched. Matching is by
/// <see cref="Notrelix.Application.Common.Search.SearchTextFolding"/> over the Item name and key and over the
/// Board name. Workspaces never reads Work persistence.
/// </summary>
public interface IWorkSearchFacts
{
    /// <param name="text">The typed text; shorter than the minimum answers empty.</param>
    /// <param name="itemLimit">The most Items returned; the caller's own Items come first.</param>
    /// <param name="boardLimit">The most Boards returned.</param>
    Task<WorkSearchResult> SearchAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<Guid> readableBoardIds,
        string text,
        int itemLimit,
        int boardLimit,
        CancellationToken cancellationToken);
}

/// <param name="Items">Top-level Items, each once, whether the Item itself or one of its sub-items matched.</param>
/// <param name="Boards">Boards whose name matches or that hold at least two matching Items.</param>
public sealed record WorkSearchResult(IReadOnlyList<WorkSearchItem> Items, IReadOnlyList<WorkSearchBoard> Boards)
{
    public static WorkSearchResult Empty { get; } = new([], []);
}

/// <param name="OtherAssigneeUserId">The first person in charge when it is not the caller; null when the caller is or nobody is.</param>
/// <param name="Mine">The caller is assigned to the Item.</param>
/// <param name="StatusCategory"><c>todo</c>, <c>in_progress</c>, <c>review</c> or <c>done</c>; null without a Status.</param>
/// <param name="ChildMatch">The matching sub-item's name (cut) when the Item itself did not match; null for a name or key match.</param>
public sealed record WorkSearchItem(
    Guid Id,
    string? Key,
    string Name,
    Guid BoardId,
    string BoardName,
    Guid? OtherAssigneeUserId,
    bool Mine,
    string? StatusName,
    string? StatusCategory,
    DateTimeOffset? DueAt,
    string? ChildMatch);

/// <param name="MatchedItems">Matching top-level Items of the Board (a sub-item counts for its parent), readable ones only.</param>
public sealed record WorkSearchBoard(Guid Id, string Name, Guid? SpaceId, int MatchedItems);
