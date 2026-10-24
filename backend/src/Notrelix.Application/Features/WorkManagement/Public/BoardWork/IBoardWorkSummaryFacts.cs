namespace Notrelix.Application.Features.WorkManagement.Public.BoardWork;

/// <summary>
/// Work Management-published work figures of Boards, consumed by the Workspaces Space overview
/// (CTR-GAP-WS-SPACE-OVERVIEW). The caller passes Board ids it already filtered with the Governance
/// collection visibility (GOV-029); this port never decides who may read a Board and never widens the
/// set. Only active Boards (not archived, not deleted) are returned. Workspaces never reads Work persistence.
/// </summary>
/// <remarks>
/// Item rules: an Item counts when it is not deleted and not archived. <i>Unfinished</i> = no completion time.
/// <i>Overdue</i> = unfinished with a due time before <c>now</c>. <i>Top-level</i> = no parent Item. Progress
/// is measured on top-level Items only. A Board <b>runs</b> when it has at least one unfinished Item of any level.
/// </remarks>
public interface IBoardWorkSummaryFacts
{
    /// <summary>Figures of the readable Boards placed in the given Spaces.</summary>
    /// <param name="includePrivate">False leaves out Private Boards (the "view as a member" preview).</param>
    Task<IReadOnlyList<BoardWorkSummary>> SummarizeAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> spaceIds,
        IReadOnlyCollection<Guid> readableBoardIds,
        bool includePrivate,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Number of readable active Boards per Space (Spaces without Boards are absent).</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountBoardsBySpaceAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> spaceIds,
        IReadOnlyCollection<Guid> readableBoardIds,
        bool includePrivate,
        CancellationToken cancellationToken);

    /// <summary>People with the most unfinished Items across the given Boards (most open first, then most overdue).</summary>
    Task<IReadOnlyList<PersonOpenWork>> OpenWorkByPersonAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}

/// <param name="Kind"><c>inbox</c> for an Intake Board, else <c>kanban</c> when its default (not archived) View is a Kanban View, else <c>table</c>.</param>
/// <param name="MemberCount">People who are members of the Board (meaningful for a Private Board).</param>
/// <param name="OpenItems">Unfinished Items of any level.</param>
/// <param name="TopLevelDone">Completed top-level Items.</param>
/// <param name="TopLevelTotal">All top-level Items.</param>
/// <param name="Overdue">Unfinished Items of any level past their due time.</param>
/// <param name="WaitingReview">Unfinished Items with at least one pending approval request, each Item counted once. A request aimed at the Board itself is not an Item.</param>
/// <param name="Unclaimed">Unfinished top-level Items with no assignee.</param>
/// <param name="LastActivityAt">Latest of the Board's and its Items' last change.</param>
/// <param name="TopAssignees">Up to three people with the most unfinished Items on this Board.</param>
public sealed record BoardWorkSummary(
    Guid BoardId,
    Guid SpaceId,
    string Title,
    string Kind,
    bool IsPrivate,
    int MemberCount,
    int OpenItems,
    int TopLevelDone,
    int TopLevelTotal,
    int Overdue,
    int WaitingReview,
    int Unclaimed,
    DateTimeOffset LastActivityAt,
    IReadOnlyList<PersonOpenWork> TopAssignees)
{
    public bool IsRunning => OpenItems > 0;
}

public sealed record PersonOpenWork(Guid UserId, int Open, int Overdue);

public static class BoardWorkKinds
{
    public const string Kanban = "kanban";
    public const string Table = "table";
    public const string Inbox = "inbox";
}
