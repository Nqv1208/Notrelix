using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryUnread;

/// <summary>
/// The caller marks one of their own inbox entries unread again; it stays in the list. The subject is always the principal: a
/// notification that is not theirs, already unread, done or snoozed, changes nothing and answers success, so the call is safe to repeat.
/// Any member of the Workspace may do it (<c>ViewWorkspace</c>).
/// </summary>
public record MarkInboxEntryUnreadCommand(Guid WorkspaceId, Guid NotificationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkInboxEntryUnreadCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<MarkInboxEntryUnreadCommand, Result>
{
    public async Task<Result> Handle(MarkInboxEntryUnreadCommand request, CancellationToken cancellationToken)
    {
        await inbox.MarkUnreadAsync(
            request.WorkspaceId, requestContext.UserId, request.NotificationId, clock.UtcNow, cancellationToken);
        return Result.Success();
    }
}

public class MarkInboxEntryUnreadCommandValidator : AbstractValidator<MarkInboxEntryUnreadCommand>
{
    public MarkInboxEntryUnreadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
