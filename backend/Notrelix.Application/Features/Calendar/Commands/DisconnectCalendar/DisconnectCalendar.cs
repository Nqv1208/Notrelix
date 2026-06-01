using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Application.Features.Calendar.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Calendar.Commands.DisconnectCalendar;

public record DisconnectCalendarCommand(Guid IntegrationId) : IRequest<Result>;

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    public Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
