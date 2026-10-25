using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkWorkspaceInboxRead;

/// <summary>
/// The caller marks every unread notification they hold in a Workspace read and learns how many changed.
/// Only their own notifications are touched; with none unread it changes nothing and answers zero.
/// Any member of the Workspace may do it (<c>ViewWorkspace</c>).
/// </summary>
public record MarkWorkspaceInboxReadCommand(Guid WorkspaceId)
    : ICommand<Result<MarkedReadDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed record MarkedReadDto(int Marked);

public class MarkWorkspaceInboxReadCommandHandler(
    INotificationInbox inbox,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<MarkWorkspaceInboxReadCommand, Result<MarkedReadDto>>
{
    public async Task<Result<MarkedReadDto>> Handle(MarkWorkspaceInboxReadCommand request, CancellationToken cancellationToken)
    {
        var marked = await inbox.MarkAllReadAsync(
            request.WorkspaceId, requestContext.UserId, clock.UtcNow, cancellationToken);
        return Result<MarkedReadDto>.Success(new MarkedReadDto(marked));
    }
}

public class MarkWorkspaceInboxReadCommandValidator : AbstractValidator<MarkWorkspaceInboxReadCommand>
{
    public MarkWorkspaceInboxReadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
