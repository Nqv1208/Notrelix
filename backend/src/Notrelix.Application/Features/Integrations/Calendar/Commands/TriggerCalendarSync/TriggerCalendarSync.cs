using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.TriggerCalendarSync;

public record TriggerCalendarSyncCommand(Guid IntegrationId) : ICommand<Result>;

public class TriggerCalendarSyncCommandHandler : IRequestHandler<TriggerCalendarSyncCommand, Result>
{
    public Task<Result> Handle(TriggerCalendarSyncCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
