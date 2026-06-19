using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

public record HandleCalendarWebhookCommand(string Provider, string Payload) : ICommand<Result>;

public class HandleCalendarWebhookCommandHandler : IRequestHandler<HandleCalendarWebhookCommand, Result>
{
    public Task<Result> Handle(HandleCalendarWebhookCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
