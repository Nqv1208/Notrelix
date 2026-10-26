namespace Notrelix.Application.Features.Analytics.Reports;

/// <summary>
/// How many weeks of completions a work report covers, ending with the week of "now". The default is the
/// eight weeks the report always had; a caller may ask for a shorter or a longer period within the bounds,
/// never an unbounded one (a week costs one bucket of one aggregate, so the bound keeps the answer small).
/// </summary>
public static class WorkReportPeriod
{
    public const int DefaultWeeks = 8;
    public const int MinWeeks = 1;
    public const int MaxWeeks = 26;

    public static bool IsValid(int weeks) => weeks is >= MinWeeks and <= MaxWeeks;
}
