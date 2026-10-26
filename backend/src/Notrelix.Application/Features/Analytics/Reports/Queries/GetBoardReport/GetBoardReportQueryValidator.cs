namespace Notrelix.Application.Features.Analytics.Reports.Queries.GetBoardReport;

public class GetBoardReportQueryValidator : AbstractValidator<GetBoardReportQuery>
{
    public GetBoardReportQueryValidator()
    {
        RuleFor(x => x.Weeks)
            .InclusiveBetween(WorkReportPeriod.MinWeeks, WorkReportPeriod.MaxWeeks);
    }
}
