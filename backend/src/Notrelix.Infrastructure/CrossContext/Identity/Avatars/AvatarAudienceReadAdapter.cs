using Notrelix.Application.Features.Identity.Profiles.Ports;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.CrossContext.Identity.Avatars;

/// <summary>
/// Answers the Identity-owned avatar audience question from Workspaces membership: two people see
/// each other's photo when both are active members of at least one common Workspace. One indexed
/// query; the caller's identity is the authenticated principal passed in by the query, and RLS
/// still limits the rows to Workspaces the viewer can access.
/// </summary>
public sealed class AvatarAudienceReadAdapter : IAvatarAudienceReadPort
{
    private readonly IWorkspaceDbContext _workspaceContext;

    public AvatarAudienceReadAdapter(IWorkspaceDbContext workspaceContext)
    {
        _workspaceContext = workspaceContext;
    }

    public Task<bool> ShareActiveWorkspaceAsync(Guid viewerId, Guid ownerId, CancellationToken cancellationToken) =>
        // The request is global (no Workspace), so the EF tenant filter of the membership set would hide
        // every row. Both sides are fixed to the viewer and the owner and only active memberships count.
        _workspaceContext.WorkspaceMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(viewer => viewer.UserId == viewerId && viewer.Status == WorkspaceMemberStatus.Active)
            .AnyAsync(viewer => _workspaceContext.WorkspaceMembers.IgnoreQueryFilters().Any(owner =>
                owner.WorkspaceId == viewer.WorkspaceId
                && owner.UserId == ownerId
                && owner.Status == WorkspaceMemberStatus.Active), cancellationToken);
}
