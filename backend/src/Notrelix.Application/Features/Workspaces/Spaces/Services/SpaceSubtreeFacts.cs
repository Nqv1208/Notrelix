using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.SpaceSubtree;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Producer-owned implementation of <see cref="ISpaceSubtreeFacts"/>. One query reads the Workspace's
/// (id, parent) pairs, which is bounded by the number of Spaces, and the subtree is walked in memory.
/// </summary>
public sealed class SpaceSubtreeFacts : ISpaceSubtreeFacts
{
    private readonly IWorkspaceDbContext _context;

    public SpaceSubtreeFacts(IWorkspaceDbContext context) => _context = context;

    public async Task<IReadOnlyList<Guid>?> GetSubtreeAsync(
        Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var edges = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null)
            .Select(s => new { s.Id, s.ParentSpaceId })
            .ToListAsync(cancellationToken);

        if (edges.All(e => e.Id != spaceId))
            return null;

        var children = edges
            .Where(e => e.ParentSpaceId != null)
            .GroupBy(e => e.ParentSpaceId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());

        var result = new List<Guid> { spaceId };
        for (var i = 0; i < result.Count; i++)
        {
            if (children.TryGetValue(result[i], out var below))
                result.AddRange(below.Where(id => !result.Contains(id)));
        }

        return result;
    }
}
