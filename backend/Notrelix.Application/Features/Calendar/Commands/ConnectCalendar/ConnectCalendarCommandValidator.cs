using FluentValidation;

namespace Notrelix.Application.Features.Calendar.Commands.ConnectCalendar;

public class ConnectCalendarCommandValidator : AbstractValidator<ConnectCalendarCommand>
{
    public ConnectCalendarCommandValidator()
    {
    }
}
