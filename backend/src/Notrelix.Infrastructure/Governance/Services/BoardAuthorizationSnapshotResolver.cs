using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.Governance.Services;

public sealed class BoardAuthorizationSnapshotResolver : IResourceAuthorizationSnapshotResolver
{
    private static readonly ResourceKind BoardKind = ResourceKind.Create("work-management.board");
    private readonly IWorkManagementDbContext _context;

    public BoardAuthorizationSnapshotResolver(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public ResourceKind ResourceKind => BoardKind;

    public async Task<ResourceAuthorizationSnapshot?> ResolveAsync(
        Guid resourceId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .Where(x => x.Id == resourceId && !x.IsArchived)
            .Select(x => new { x.WorkspaceId, x.Visibility })
            .FirstOrDefaultAsync(cancellationToken);

        if (board is null)
        {
            return null;
        }

        var memberRole = await _context.BoardMembers
            .AsNoTracking()
            .Where(x => x.BoardId == resourceId && x.UserId == actorId)
            .Select(x => (BoardRole?)x.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return new ResourceAuthorizationSnapshot(
            board.WorkspaceId,
            board.Visibility == BoardVisibility.Workspace
                ? ResourceAudience.Workspace
                : ResourceAudience.Restricted,
            memberRole switch
            {
                BoardRole.Observer => ResourceMemberAccess.Viewer,
                BoardRole.Member => ResourceMemberAccess.Editor,
                BoardRole.Admin => ResourceMemberAccess.Manager,
                _ => (ResourceMemberAccess?)null
            });
    }
}
