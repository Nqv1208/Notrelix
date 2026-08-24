using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

public record DisconnectCalendarCommand(Guid IntegrationId) : ICommand<Result>, IAuthenticatedRequest, INoDataRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("integrations.calendar-integration"), IntegrationId);
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
}

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    public Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
