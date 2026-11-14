namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.MoveDashboardWidget;

public class MoveDashboardWidgetCommandValidator : AbstractValidator<MoveDashboardWidgetCommand>
{
    public MoveDashboardWidgetCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
        RuleFor(x => x.WidgetId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
