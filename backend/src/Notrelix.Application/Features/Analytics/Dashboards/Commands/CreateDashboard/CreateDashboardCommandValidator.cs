namespace Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;

public class CreateDashboardCommandValidator : AbstractValidator<CreateDashboardCommand>
{
    public const int MaxNameLength = 120;

    public CreateDashboardCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxNameLength)
            .WithMessage($"A dashboard name is 1 to {MaxNameLength} characters.");
    }
}
