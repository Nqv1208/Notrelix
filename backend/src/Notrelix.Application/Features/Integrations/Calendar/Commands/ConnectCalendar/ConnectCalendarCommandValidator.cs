namespace Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;

public class ConnectCalendarCommandValidator : AbstractValidator<ConnectCalendarCommand>
{
    public ConnectCalendarCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.AccessToken)
            .NotEmpty();

        RuleFor(x => x.SyncDirection)
            .NotEmpty()
            .Must(d => Enum.TryParse<CalendarSyncDirection>(d, ignoreCase: true, out _))
            .WithMessage("SyncDirection must be one of: Push, Pull, Both.");

        RuleFor(x => x.WorkspaceId)
            .NotEmpty();

        RuleFor(x => x.Provider)
            .Must(p => Enum.TryParse<IntegrationProvider>(p, ignoreCase: true, out _))
            .WithMessage("Provider is not a supported integration provider.")
            .When(x => !string.IsNullOrWhiteSpace(x.Provider));
    }
}
