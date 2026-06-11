using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Commands.DisconnectCalendar;

public record DisconnectCalendarCommand(Guid IntegrationId) : IRequest<Result>;

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    public Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
