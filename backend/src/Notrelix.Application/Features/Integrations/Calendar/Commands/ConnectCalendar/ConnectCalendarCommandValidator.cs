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
            .MaximumLength(50);
    }
}
