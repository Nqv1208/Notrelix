using FluentValidation;

namespace Notrelix.Application.Features.Calendar.Commands.HandleCalendarWebhook;

public class HandleCalendarWebhookCommandValidator : AbstractValidator<HandleCalendarWebhookCommand>
{
    public HandleCalendarWebhookCommandValidator()
    {
    }
}
