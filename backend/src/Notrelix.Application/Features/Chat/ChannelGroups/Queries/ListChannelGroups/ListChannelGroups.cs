using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.ChannelGroups.Queries.ListChannelGroups;

/// <summary>
/// The sidebar categories of the Workspace with their names (UC-CHT-A02, A04), in sidebar order. The channel list carries only a
/// group id, so this read gives the client the name to show. A category carries no access meaning, so every Workspace member reads
/// them all; a Guest reads only the categories that hold a channel they are an active member of, because a Guest has no Workspace
/// directory and must not learn the shape of the Workspace from category names.
/// </summary>
public record ListChannelGroupsQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<ChannelGroupDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListChannelGroupsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListChannelGroupsQuery, Result<IReadOnlyList<ChannelGroupDto>>>
{
    public async Task<Result<IReadOnlyList<ChannelGroupDto>>> Handle(ListChannelGroupsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var groups = await db.ChatChannelGroups
            .Where(g => g.WorkspaceId == request.WorkspaceId)
            .ToListAsync(cancellationToken);

        if (ChannelAccess.IsGuest(caller))
        {
            var mine = await db.ChatMemberships
                .Where(m => m.UserId == userId && m.WorkspaceId == request.WorkspaceId && m.LeftAt == null)
                .Select(m => m.SpaceId)
                .ToListAsync(cancellationToken);
            var visibleGroupIds = (await db.ChatSpaces
                .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null && s.GroupId != null && mine.Contains(s.Id))
                .Select(s => s.GroupId!.Value)
                .ToListAsync(cancellationToken)).ToHashSet();
            groups = groups.Where(g => visibleGroupIds.Contains(g.Id)).ToList();
        }

        return Result<IReadOnlyList<ChannelGroupDto>>.Success(
            groups.OrderBy(g => g.Position).ThenBy(g => g.Name, StringComparer.Ordinal).Select(ChannelGroupDto.From).ToList());
    }
}
