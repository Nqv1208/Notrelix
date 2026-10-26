using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryRead;

/// <summary>
/// The caller marks one of their own notifications read. The subject is always the principal: a notification
/// that is not theirs, or already read, changes nothing and answers success, so the call is safe to repeat.
/// Any member of the Workspace may do it (<c>ViewWorkspace</c>).
/// </summary>
public record MarkInboxEntryReadCommand(Guid WorkspaceId, Guid NotificationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkInboxEntryReadCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<MarkInboxEntryReadCommand, Result>
{
    public async Task<Result> Handle(MarkInboxEntryReadCommand request, CancellationToken cancellationToken)
    {
        await inbox.MarkReadAsync(
            request.WorkspaceId, requestContext.UserId, request.NotificationId, clock.UtcNow, cancellationToken);
        return Result.Success();
    }
}

public class MarkInboxEntryReadCommandValidator : AbstractValidator<MarkInboxEntryReadCommand>
{
    public MarkInboxEntryReadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
