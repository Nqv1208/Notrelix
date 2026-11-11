using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Common.Time;

namespace Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;

/// <summary>
/// The caller's unread counts per space and in total (UC-CHT-D01). Covers the spaces the caller is an active member of in
/// the Workspace. A space the caller is not in is not listed, and the caller reads nothing of it.
/// </summary>
public record GetUnreadSummaryQuery(Guid WorkspaceId)
    : IQuery<Result<UnreadSummaryDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetUnreadSummaryQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<GetUnreadSummaryQuery, Result<UnreadSummaryDto>>
{
    public async Task<Result<UnreadSummaryDto>> Handle(GetUnreadSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var summary = await UnreadSummaryBuilder.BuildAsync(db, request.WorkspaceId, userId, clock.UtcNow, cancellationToken);
        return Result<UnreadSummaryDto>.Success(summary);
    }
}
