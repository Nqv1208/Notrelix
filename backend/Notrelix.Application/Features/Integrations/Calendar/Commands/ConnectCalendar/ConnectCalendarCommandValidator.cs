using FluentValidation;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;

public class ConnectCalendarCommandValidator : AbstractValidator<ConnectCalendarCommand>
{
    public ConnectCalendarCommandValidator()
    {
    }
}
