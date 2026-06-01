using FluentValidation;

namespace Notrelix.Application.Features.Calendar.Commands.TriggerCalendarSync;

public class TriggerCalendarSyncCommandValidator : AbstractValidator<TriggerCalendarSyncCommand>
{
    public TriggerCalendarSyncCommandValidator()
    {
    }
}
