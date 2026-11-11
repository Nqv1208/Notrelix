using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.ReadState.Commands.MarkAllRead;

/// <summary>
/// Moves every read boundary of the caller in the Workspace to the latest sequence of its space (UC-CHT-D07). Messages sent
/// while the request runs stay unread. Returns the summary after the change.
/// </summary>
public record MarkAllReadCommand(Guid WorkspaceId)
    : ICommand<Result<UnreadSummaryDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkAllReadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatReadStateStore readStates,
    IDateTimeProvider clock) : IRequestHandler<MarkAllReadCommand, Result<UnreadSummaryDto>>
{
    public async Task<Result<UnreadSummaryDto>> Handle(MarkAllReadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        await readStates.AdvanceAllAsync(request.WorkspaceId, userId, cancellationToken);
        var summary = await UnreadSummaryBuilder.BuildAsync(db, request.WorkspaceId, userId, clock.UtcNow, cancellationToken);
        return Result<UnreadSummaryDto>.Success(summary);
    }
}
