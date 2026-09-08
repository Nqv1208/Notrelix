namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

public class HandleCalendarWebhookCommandValidator : AbstractValidator<HandleCalendarWebhookCommand>
{
    public HandleCalendarWebhookCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Signature)
            .NotEmpty();

        RuleFor(x => x.Timestamp)
            .NotEmpty();

        RuleFor(x => x.RawBody)
            .NotEmpty()
            .MaximumLength(65536);
    }
}
