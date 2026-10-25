using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryDone;

/// <summary>
/// The caller marks one of their own notifications done: it is read and leaves the default inbox list, and
/// "needs your reply" ends. A notification that is not theirs, or already done, changes nothing and answers
/// success. Any member of the Workspace may do it (<c>ViewWorkspace</c>).
/// </summary>
public record MarkInboxEntryDoneCommand(Guid WorkspaceId, Guid NotificationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkInboxEntryDoneCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<MarkInboxEntryDoneCommand, Result>
{
    public async Task<Result> Handle(MarkInboxEntryDoneCommand request, CancellationToken cancellationToken)
    {
        await inbox.MarkDoneAsync(
            request.WorkspaceId, requestContext.UserId, request.NotificationId, clock.UtcNow, cancellationToken);
        return Result.Success();
    }
}

public class MarkInboxEntryDoneCommandValidator : AbstractValidator<MarkInboxEntryDoneCommand>
{
    public MarkInboxEntryDoneCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
