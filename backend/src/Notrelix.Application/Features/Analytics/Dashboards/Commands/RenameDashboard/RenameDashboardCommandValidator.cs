using Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;

namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.RenameDashboard;

public class RenameDashboardCommandValidator : AbstractValidator<RenameDashboardCommand>
{
    public RenameDashboardCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DashboardId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= CreateDashboardCommandValidator.MaxNameLength)
            .WithMessage($"A dashboard name is 1 to {CreateDashboardCommandValidator.MaxNameLength} characters.");
    }
}
