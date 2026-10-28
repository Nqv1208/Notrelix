using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Public.Watchers;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Watchers;

/// <summary>
/// Collaboration-owned implementation of the follower read. Runs under the caller's tenant: the
/// workspace-scoped row-level policy of the watcher table bounds every read to the Workspace in scope.
/// </summary>
public sealed class PostgresResourceFollowers : IResourceFollowers
{
    private readonly ICollaborationDbContext _context;

    public PostgresResourceFollowers(ICollaborationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> FollowerIdsAsync(string kind, Guid resourceId, CancellationToken cancellationToken)
    {
        var resourceKind = ResourceKind.Create(kind);
        return await _context.ResourceWatchers
            .AsNoTracking()
            .Where(w => w.Target.Kind == resourceKind && w.Target.ResourceId == resourceId)
            .Select(w => w.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FollowedResourceIdsAsync(
        string kind, Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken)
    {
        var resourceKind = ResourceKind.Create(kind);
        return await _context.ResourceWatchers
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.WorkspaceId == workspaceId && w.Target.Kind == resourceKind)
            .OrderByDescending(w => w.CreatedAt)
            .ThenBy(w => w.Id)
            .Select(w => w.Target.ResourceId)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
