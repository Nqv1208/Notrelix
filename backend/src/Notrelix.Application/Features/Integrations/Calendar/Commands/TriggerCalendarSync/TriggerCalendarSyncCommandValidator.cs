using FluentValidation;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.TriggerCalendarSync;

public class TriggerCalendarSyncCommandValidator : AbstractValidator<TriggerCalendarSyncCommand>
{
    public TriggerCalendarSyncCommandValidator()
    {
        RuleFor(x => x.IntegrationId)
            .NotEmpty();
    }
}
