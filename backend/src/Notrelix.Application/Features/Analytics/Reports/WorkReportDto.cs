using Notrelix.Application.Features.WorkManagement.Public.Reports;

namespace Notrelix.Application.Features.Analytics.Reports;

/// <summary>Items holding one Status. A single-Board report names the option; a workspace report merges by category.</summary>
/// <param name="Category"><c>Todo</c>, <c>InProgress</c>, <c>Review</c>, <c>Done</c> or <c>None</c> (no Status).</param>
public sealed record ReportStatusDto(Guid? OptionId, string? Name, string? Color, string Category, int Count);

/// <param name="UserId">The assignee; null for Items nobody is assigned to.</param>
/// <param name="Name">The assignee's display name; null for the unassigned bucket.</param>
public sealed record ReportAssigneeDto(Guid? UserId, string? Name, int Items, int Open, int Overdue);

/// <param name="WeekStart">Monday 00:00 UTC of the week.</param>
public sealed record ReportWeekDto(DateTimeOffset WeekStart, int Completed);

/// <summary>
/// The read-only work measures of a set of Boards: Items by Status, by assignee, the overdue count, the
/// completions of each of the last eight weeks and the mean time from creation to completion.
/// </summary>
/// <param name="AverageCompletionSeconds">Mean time from creation to completion; null when nothing is completed.</param>
public sealed record WorkReportDto(
    int Total,
    int Open,
    int Done,
    int Overdue,
    IReadOnlyList<ReportStatusDto> ByStatus,
    IReadOnlyList<ReportAssigneeDto> ByAssignee,
    IReadOnlyList<ReportWeekDto> CompletedPerWeek,
    double? AverageCompletionSeconds);

public sealed record BoardReportDto(Guid BoardId, DateTimeOffset GeneratedAt, WorkReportDto Report);

/// <param name="BoardCount">Readable Boards the report covers.</param>
/// <param name="SpaceId">The department Space the report was narrowed to; null for the whole Workspace.</param>
public sealed record WorkspaceReportDto(
    DateTimeOffset GeneratedAt,
    int BoardCount,
    Guid? SpaceId,
    WorkReportDto Report);

internal static class WorkReportMapper
{
    public const int Weeks = 8;

    public static async Task<WorkReportDto> MapAsync(
        WorkReportFact fact,
        IActorLookupService actors,
        CancellationToken cancellationToken)
    {
        var ids = fact.ByAssignee.Where(a => a.UserId != null).Select(a => a.UserId!.Value).Distinct().ToList();
        var names = ids.Count == 0
            ? new Dictionary<Guid, string>()
            : (await actors.FindManyAsync(ids, cancellationToken)).ToDictionary(a => a.UserId, a => a.Name);

        return new WorkReportDto(
            fact.Total,
            fact.Open,
            fact.Done,
            fact.Overdue,
            fact.ByStatus.Select(s => new ReportStatusDto(s.OptionId, s.Name, s.Color, s.Category, s.Count)).ToList(),
            fact.ByAssignee
                .Select(a => new ReportAssigneeDto(
                    a.UserId,
                    a.UserId is { } id ? names.GetValueOrDefault(id) ?? "Unknown" : null,
                    a.Items,
                    a.Open,
                    a.Overdue))
                .ToList(),
            fact.CompletedPerWeek.Select(w => new ReportWeekDto(w.WeekStart, w.Completed)).ToList(),
            fact.AverageCompletionSeconds);
    }
}
