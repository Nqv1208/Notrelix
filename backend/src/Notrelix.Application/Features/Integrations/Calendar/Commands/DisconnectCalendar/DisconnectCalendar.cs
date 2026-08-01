using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

public record DisconnectCalendarCommand(Guid IntegrationId) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.CalendarIntegration, IntegrationId);
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
}

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    public Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
