using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Blocks.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Blocks.Queries.ListPersonBlocks;

/// <summary>
/// The people the caller has blocked in the Workspace, newest first (UC-CHT-E06). It is the caller's own list only: nobody reads
/// another person's blocks, and the blocked person never learns of it. A Guest has no Workspace directory (CHT-004), so a Guest
/// sees only the blocks of people they share a live space with; a block that fell out of reach stays stored and can still be lifted
/// by id, but is not listed.
/// </summary>
public record ListPersonBlocksQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<BlockedPersonDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListPersonBlocksQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListPersonBlocksQuery, Result<IReadOnlyList<BlockedPersonDto>>>
{
    public async Task<Result<IReadOnlyList<BlockedPersonDto>>> Handle(ListPersonBlocksQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var blocks = await db.ChatBlocks.AsNoTracking()
            .Where(b => b.WorkspaceId == request.WorkspaceId && b.BlockerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.BlockedId)
            .ToListAsync(cancellationToken);

        if (ChannelAccess.IsGuest(caller) && blocks.Count > 0)
        {
            // One query for all of them: the blocked people who are active in a live space the Guest is also active in.
            var blockedIds = blocks.Select(b => b.BlockedId).ToList();
            var reachable = (await db.ChatMemberships.AsNoTracking()
                .Where(o => o.WorkspaceId == request.WorkspaceId && o.LeftAt == null && blockedIds.Contains(o.UserId)
                    && db.ChatMemberships.Any(m => m.SpaceId == o.SpaceId && m.UserId == userId && m.LeftAt == null)
                    && db.ChatSpaces.Any(s => s.Id == o.SpaceId && s.DeletedAt == null))
                .Select(o => o.UserId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
            blocks = blocks.Where(b => reachable.Contains(b.BlockedId)).ToList();
        }

        return Result<IReadOnlyList<BlockedPersonDto>>.Success(
            blocks.Select(b => new BlockedPersonDto(b.BlockedId, b.CreatedAt)).ToList());
    }
}
