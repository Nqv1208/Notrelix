using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Commands.TriggerCalendarSync;

public record TriggerCalendarSyncCommand(Guid IntegrationId) : IRequest<Result>;

public class TriggerCalendarSyncCommandHandler : IRequestHandler<TriggerCalendarSyncCommand, Result>
{
    public Task<Result> Handle(TriggerCalendarSyncCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
