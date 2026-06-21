using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.TriggerCalendarSync;

public record TriggerCalendarSyncCommand(Guid IntegrationId) : ICommand<Result>;

public class TriggerCalendarSyncCommandHandler : IRequestHandler<TriggerCalendarSyncCommand, Result>
{
    public Task<Result> Handle(TriggerCalendarSyncCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
