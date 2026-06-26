using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

public record DisconnectCalendarCommand(Guid IntegrationId) : ICommand<Result>;

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    public Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
