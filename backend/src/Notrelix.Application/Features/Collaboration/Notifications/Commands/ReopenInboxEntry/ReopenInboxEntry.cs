using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.ReopenInboxEntry;

/// <summary>
/// The caller takes one of their own notifications back into the inbox: it is no longer done and no longer
/// snoozed. A notification that is not theirs, or already in the inbox, changes nothing and answers success.
/// Any member of the Workspace may do it (<c>ViewWorkspace</c>).
/// </summary>
public record ReopenInboxEntryCommand(Guid WorkspaceId, Guid NotificationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ReopenInboxEntryCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<ReopenInboxEntryCommand, Result>
{
    public async Task<Result> Handle(ReopenInboxEntryCommand request, CancellationToken cancellationToken)
    {
        await inbox.ReopenAsync(
            request.WorkspaceId, requestContext.UserId, request.NotificationId, clock.UtcNow, cancellationToken);
        return Result.Success();
    }
}

public class ReopenInboxEntryCommandValidator : AbstractValidator<ReopenInboxEntryCommand>
{
    public ReopenInboxEntryCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
