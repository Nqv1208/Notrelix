namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RestoreDashboard;

public class RestoreDashboardCommandValidator : AbstractValidator<RestoreDashboardCommand>
{
    public RestoreDashboardCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
