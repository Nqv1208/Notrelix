using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.SnoozeInboxEntry;

/// <summary>
/// The caller hides one of their own notifications until a later time. It leaves the default list, and when
/// the time has passed it is listed again, unread; no scheduler wakes it, the listing evaluates the time when
/// it is read. The time must lie in the future and within <see cref="MaxSnoozeDays"/> days. A notification
/// that is not theirs changes nothing and answers success. Any member of the Workspace may do it
/// (<c>ViewWorkspace</c>).
/// </summary>
public record SnoozeInboxEntryCommand(Guid WorkspaceId, Guid NotificationId, DateTimeOffset Until)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public const int MaxSnoozeDays = 365;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SnoozeInboxEntryCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<SnoozeInboxEntryCommand, Result>
{
    public async Task<Result> Handle(SnoozeInboxEntryCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (request.Until <= now)
        {
            return Result.Failure("A snooze must end in the future.");
        }

        if (request.Until > now.AddDays(SnoozeInboxEntryCommand.MaxSnoozeDays))
        {
            return Result.Failure($"A snooze can last at most {SnoozeInboxEntryCommand.MaxSnoozeDays} days.");
        }

        await inbox.SnoozeAsync(
            request.WorkspaceId, requestContext.UserId, request.NotificationId, request.Until.ToUniversalTime(), now, cancellationToken);
        return Result.Success();
    }
}

public class SnoozeInboxEntryCommandValidator : AbstractValidator<SnoozeInboxEntryCommand>
{
    public SnoozeInboxEntryCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.Until).NotEmpty();
    }
}
