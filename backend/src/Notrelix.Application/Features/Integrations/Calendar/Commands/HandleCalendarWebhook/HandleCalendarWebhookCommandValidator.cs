using FluentValidation;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

public class HandleCalendarWebhookCommandValidator : AbstractValidator<HandleCalendarWebhookCommand>
{
    public HandleCalendarWebhookCommandValidator()
    {
    }
}
