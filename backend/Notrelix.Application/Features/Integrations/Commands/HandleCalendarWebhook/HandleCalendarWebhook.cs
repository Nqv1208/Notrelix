using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Commands.HandleCalendarWebhook;

public record HandleCalendarWebhookCommand(string Provider, string Payload) : IRequest<Result>;

public class HandleCalendarWebhookCommandHandler : IRequestHandler<HandleCalendarWebhookCommand, Result>
{
    public Task<Result> Handle(HandleCalendarWebhookCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
