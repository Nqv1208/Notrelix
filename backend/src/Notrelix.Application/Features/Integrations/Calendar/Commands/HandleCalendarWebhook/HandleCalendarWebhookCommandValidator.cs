namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

public class HandleCalendarWebhookCommandValidator : AbstractValidator<HandleCalendarWebhookCommand>
{
    public HandleCalendarWebhookCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Payload)
            .NotEmpty();
    }
}
