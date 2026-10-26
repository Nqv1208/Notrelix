namespace Notrelix.Application.Features.WorkManagement.Public.Reports;

/// <summary>
/// Work Management-published work measures over a set of Boards, consumed by the Analytics board and
/// workspace reports. The caller passes the Board ids it already filtered with the Governance collection
/// visibility (GOV-029); this port never decides who may read a Board and never widens the set. Every
/// measure is aggregated by the datastore (GROUP BY over the Boards' Items); no Item is loaded.
/// </summary>
/// <remarks>
/// An Item counts when it is not deleted and not archived, at any level. <i>Open</i> = no completion time.
/// <i>Overdue</i> = open with a due time before <c>now</c>. Weeks are seven-day windows that start on Monday
/// (UTC); the last one is the week of <c>now</c>.
/// </remarks>
public interface IWorkReportFacts
{
    /// <param name="boardIds">Readable Boards the report covers (an empty set gives an empty report).</param>
    /// <param name="weeks">How many weeks of completions to report, ending with the week of <paramref name="now"/>.</param>
    Task<WorkReportFact> ReportAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        DateTimeOffset now,
        int weeks,
        CancellationToken cancellationToken);

    /// <summary>
    /// The Boards among <paramref name="boardIds"/> that are placed in one of the given Spaces. Never adds a
    /// Board the caller did not pass.
    /// </summary>
    Task<IReadOnlyList<Guid>> BoardIdsInSpacesAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken);
}

public static class WorkReportStatusCategories
{
    public const string Todo = "Todo";
    public const string InProgress = "InProgress";
    public const string Review = "Review";
    public const string Done = "Done";

    /// <summary>The Item holds no Status option.</summary>
    public const string None = "None";
}

/// <param name="OptionId">The Status option when the report covers a single Board; null when options are merged by category.</param>
/// <param name="Name">The option name for a single-Board report; null when merged by category.</param>
/// <param name="Color">The option colour for a single-Board report; null when merged by category.</param>
/// <param name="Category">One of <see cref="WorkReportStatusCategories"/>.</param>
public sealed record WorkReportStatusFact(Guid? OptionId, string? Name, string? Color, string Category, int Count);

/// <param name="UserId">The assignee; null for Items nobody is assigned to.</param>
/// <param name="Items">Items assigned to the person (an Item with two assignees counts for both).</param>
/// <param name="Open">Open Items assigned to the person.</param>
/// <param name="Overdue">Open Items past their due time assigned to the person.</param>
public sealed record WorkReportAssigneeFact(Guid? UserId, int Items, int Open, int Overdue);

/// <param name="WeekStart">Monday 00:00 UTC of the week.</param>
public sealed record WorkReportWeekFact(DateTimeOffset WeekStart, int Completed);

/// <param name="AverageCompletionSeconds">Mean time from creation to completion of the completed Items; null when none.</param>
public sealed record WorkReportFact(
    int Total,
    int Open,
    int Done,
    int Overdue,
    IReadOnlyList<WorkReportStatusFact> ByStatus,
    IReadOnlyList<WorkReportAssigneeFact> ByAssignee,
    IReadOnlyList<WorkReportWeekFact> CompletedPerWeek,
    double? AverageCompletionSeconds);
