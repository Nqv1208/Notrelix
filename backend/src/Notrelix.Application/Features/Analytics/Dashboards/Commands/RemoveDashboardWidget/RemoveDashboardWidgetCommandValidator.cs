namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RemoveDashboardWidget;

public class RemoveDashboardWidgetCommandValidator : AbstractValidator<RemoveDashboardWidgetCommand>
{
    public RemoveDashboardWidgetCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
        RuleFor(x => x.WidgetId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
