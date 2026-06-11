using FluentValidation;

namespace Notrelix.Application.Features.Integrations.Commands.HandleCalendarWebhook;

public class HandleCalendarWebhookCommandValidator : AbstractValidator<HandleCalendarWebhookCommand>
{
    public HandleCalendarWebhookCommandValidator()
    {
    }
}
