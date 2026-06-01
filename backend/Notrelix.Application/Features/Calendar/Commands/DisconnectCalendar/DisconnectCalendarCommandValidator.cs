using FluentValidation;

namespace Notrelix.Application.Features.Calendar.Commands.DisconnectCalendar;

public class DisconnectCalendarCommandValidator : AbstractValidator<DisconnectCalendarCommand>
{
    public DisconnectCalendarCommandValidator()
    {
    }
}
