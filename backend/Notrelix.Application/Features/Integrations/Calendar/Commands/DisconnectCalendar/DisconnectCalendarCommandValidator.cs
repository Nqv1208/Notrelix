using FluentValidation;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

public class DisconnectCalendarCommandValidator : AbstractValidator<DisconnectCalendarCommand>
{
    public DisconnectCalendarCommandValidator()
    {
    }
}
