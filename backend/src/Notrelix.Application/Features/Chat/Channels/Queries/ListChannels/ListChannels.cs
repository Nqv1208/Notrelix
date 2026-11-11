using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;

/// <summary>
/// The channels the caller can see in the Workspace. By default their own channels (the sidebar). With
/// <see cref="Browse"/> set, the public channels they are not yet in (Browse channels); a Guest browses nothing.
/// Archived channels are listed with their state so the client can show them read-only.
/// </summary>
public record ListChannelsQuery(Guid WorkspaceId, bool Browse = false)
    : IQuery<Result<IReadOnlyList<ChannelDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListChannelsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListChannelsQuery, Result<IReadOnlyList<ChannelDto>>>
{
    public async Task<Result<IReadOnlyList<ChannelDto>>> Handle(ListChannelsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var myMemberships = await db.ChatMemberships
            .Where(m => m.UserId == userId && m.WorkspaceId == request.WorkspaceId && m.LeftAt == null)
            .ToListAsync(cancellationToken);
        var mine = myMemberships.ToDictionary(m => m.SpaceId);

        List<Space> spaces;
        if (request.Browse)
        {
            if (string.Equals(caller.Role, ChannelAccess.GuestRole, StringComparison.Ordinal))
                return Result<IReadOnlyList<ChannelDto>>.Success([]);

            spaces = await db.ChatSpaces
                .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null && s.Kind == SpaceKind.ChannelPublic)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Conversations have their own list (UC-CHT-E05); the channel list holds channels only.
            var ids = mine.Keys.ToArray();
            spaces = await db.ChatSpaces
                .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null && ids.Contains(s.Id)
                    && (s.Kind == SpaceKind.ChannelPublic || s.Kind == SpaceKind.ChannelPrivate))
                .ToListAsync(cancellationToken);
        }

        var result = spaces
            .OrderBy(s => s.Position)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => ChannelDto.From(s, mine.GetValueOrDefault(s.Id)))
            .ToList();

        return Result<IReadOnlyList<ChannelDto>>.Success(result);
    }
}
