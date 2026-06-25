using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

public record HandleCalendarWebhookCommand(string Provider, string Payload) : ICommand<Result>;

public class HandleCalendarWebhookCommandHandler : IRequestHandler<HandleCalendarWebhookCommand, Result>
{
    public Task<Result> Handle(HandleCalendarWebhookCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
