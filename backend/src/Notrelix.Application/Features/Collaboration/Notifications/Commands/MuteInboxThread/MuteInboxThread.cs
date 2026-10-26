using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.MuteInboxThread;

/// <summary>
/// The caller mutes the thread (the Item or page) one of their own notifications is about: nothing new from
/// that thread reaches their inbox. Earlier notifications stay. A repeat changes nothing, and a notification
/// that is not theirs, or has no thread, changes nothing and answers success. Any member of the Workspace may
/// do it (<c>ViewWorkspace</c>).
/// </summary>
public record MuteInboxThreadCommand(Guid WorkspaceId, Guid NotificationId, bool Muted)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MuteInboxThreadCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<MuteInboxThreadCommand, Result>
{
    public async Task<Result> Handle(MuteInboxThreadCommand request, CancellationToken cancellationToken)
    {
        if (request.Muted)
        {
            await inbox.MuteThreadAsync(
                request.WorkspaceId, requestContext.UserId, request.NotificationId, clock.UtcNow, cancellationToken);
        }
        else
        {
            await inbox.UnmuteThreadAsync(
                request.WorkspaceId, requestContext.UserId, request.NotificationId, cancellationToken);
        }

        return Result.Success();
    }
}

public class MuteInboxThreadCommandValidator : AbstractValidator<MuteInboxThreadCommand>
{
    public MuteInboxThreadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
