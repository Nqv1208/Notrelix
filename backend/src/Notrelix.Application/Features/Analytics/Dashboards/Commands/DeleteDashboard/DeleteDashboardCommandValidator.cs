namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.DeleteDashboard;

public class DeleteDashboardCommandValidator : AbstractValidator<DeleteDashboardCommand>
{
    public DeleteDashboardCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
