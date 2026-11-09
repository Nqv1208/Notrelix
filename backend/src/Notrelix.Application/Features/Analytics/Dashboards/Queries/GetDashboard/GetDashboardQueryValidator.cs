namespace Notrelix.Application.Features.Analytics.Dashboards.Queries.GetDashboard;

public sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
    }
}
