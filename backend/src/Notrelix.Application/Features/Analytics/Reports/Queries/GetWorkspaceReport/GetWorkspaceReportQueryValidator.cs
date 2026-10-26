namespace Notrelix.Application.Features.Analytics.Reports.Queries.GetWorkspaceReport;

public class GetWorkspaceReportQueryValidator : AbstractValidator<GetWorkspaceReportQuery>
{
    public GetWorkspaceReportQueryValidator()
    {
        RuleFor(x => x.Weeks)
            .InclusiveBetween(WorkReportPeriod.MinWeeks, WorkReportPeriod.MaxWeeks);
    }
}
