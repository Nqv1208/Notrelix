using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;

/// <summary>One channel with its active members. A channel the caller may not see is the same NotFound as a missing one.</summary>
public record GetChannelQuery(Guid WorkspaceId, Guid ChannelId)
    : IQuery<Result<ChannelDetailDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetChannelQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatResourceViews views) : IRequestHandler<GetChannelQuery, Result<ChannelDetailDto>>
{
    public async Task<Result<ChannelDetailDto>> Handle(GetChannelQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var memberRows = space.Memberships
            .Where(m => m.IsActive)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new ChannelMemberDto(m.UserId, m.Role.ToWire(), m.JoinedAt))
            .ToList();

        var channel = (await views.WithLinksAsync(
            [ChannelDto.From(space, space.GetActiveMembership(userId), caller.Role)],
            requestContext.RequireAccountId(), request.WorkspaceId, userId, cancellationToken))[0];
        var detail = new ChannelDetailDto(channel, memberRows);
        return Result<ChannelDetailDto>.Success(detail);
    }
}
