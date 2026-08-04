using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.TriggerCalendarSync;

public record TriggerCalendarSyncCommand(Guid IntegrationId) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("integrations.calendar-integration"), IntegrationId);
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
}

public class TriggerCalendarSyncCommandHandler : IRequestHandler<TriggerCalendarSyncCommand, Result>
{
    public Task<Result> Handle(TriggerCalendarSyncCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
