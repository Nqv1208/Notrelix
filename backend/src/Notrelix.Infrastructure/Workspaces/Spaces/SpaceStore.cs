using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Workspaces;

namespace Notrelix.Infrastructure.Workspaces.Spaces;

/// <summary>
/// PostgreSQL implementation of <see cref="ISpaceStore"/> (ADR-009).
///
/// Space writes and the closure index rows are tracked on the request
/// <see cref="ApplicationDbContext"/> and saved by the request data session in
/// one transaction. <see cref="LockHierarchyAsync"/> takes a transaction-scoped
/// advisory lock (<c>pg_advisory_xact_lock</c>) per Workspace, so the ancestry,
/// sibling and closure reads of two concurrent hierarchy writes cannot
/// interleave: a second move re-reads after the first commits, which keeps the
/// tree acyclic, sibling names unique and the closure exact. A hash collision
/// only over-serializes.
/// </summary>
public sealed class SpaceStore : ISpaceStore
{
    private readonly ApplicationDbContext _context;

    public SpaceStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LockHierarchyAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The space hierarchy lock must be acquired inside the active request transaction.");
        }

        var key = ComputeLockKey(workspaceId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    public Task<Space?> FindAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken) =>
        _context.Spaces
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == spaceId && s.WorkspaceId == workspaceId, cancellationToken);

    public void Add(Space space) => _context.Spaces.Add(space);

    public async Task<SpaceParentFact?> FindParentFactAsync(Guid parentSpaceId, CancellationToken cancellationToken)
    {
        var parent = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.Id == parentSpaceId)
            .Select(s => new { s.Id, s.WorkspaceId, s.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (parent is null)
        {
            return null;
        }

        var path = await _context.SpacePaths
            .AsNoTracking()
            .Where(p => p.DescendantId == parentSpaceId)
            .OrderByDescending(p => p.Depth)
            .Select(p => p.AncestorId)
            .ToListAsync(cancellationToken);

        return new SpaceParentFact(parent.Id, parent.WorkspaceId, parent.Status == SpaceStatus.Active, path);
    }

    public async Task<IReadOnlyList<SpaceSiblingEntry>> LoadSiblingsAsync(Guid workspaceId, Guid? parentSpaceId, CancellationToken cancellationToken) =>
        await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.ParentSpaceId == parentSpaceId)
            .Select(s => new SpaceSiblingEntry(s.Id, s.Name, s.Position))
            .ToListAsync(cancellationToken);

    public async Task<(int Active, int Existing)> CountChildSpacesAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var counts = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.ParentSpaceId == spaceId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Active = g.Count(s => s.Status == SpaceStatus.Active),
                Existing = g.Count(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? (0, 0) : (counts.Active, counts.Existing);
    }

    public async Task StageCreatedAsync(Space space, CancellationToken cancellationToken)
    {
        _context.SpacePaths.Add(new SpacePathRecord(space.AccountId, space.WorkspaceId, space.Id, space.Id, 0));

        if (space.ParentSpaceId is not { } parentId)
        {
            return;
        }

        var ancestors = await _context.SpacePaths
            .AsNoTracking()
            .Where(p => p.DescendantId == parentId)
            .Select(p => new { p.AncestorId, p.Depth })
            .ToListAsync(cancellationToken);

        foreach (var ancestor in ancestors)
        {
            _context.SpacePaths.Add(new SpacePathRecord(
                space.AccountId, space.WorkspaceId, ancestor.AncestorId, space.Id, ancestor.Depth + 1));
        }
    }

    public async Task StageMovedAsync(Space space, CancellationToken cancellationToken)
    {
        // Subtree of the moved Space (itself at depth 0 and every descendant).
        var subtree = await _context.SpacePaths
            .AsNoTracking()
            .Where(p => p.AncestorId == space.Id)
            .Select(p => new { p.DescendantId, p.Depth })
            .ToListAsync(cancellationToken);
        var depthBelow = subtree.ToDictionary(p => p.DescendantId, p => p.Depth);
        var subtreeIds = depthBelow.Keys.ToList();

        // Rows linking the subtree to its ancestors outside the subtree are the
        // only ones a move changes; rows inside the subtree are unchanged.
        var external = await _context.SpacePaths
            .Where(p => subtreeIds.Contains(p.DescendantId) && !subtreeIds.Contains(p.AncestorId))
            .ToListAsync(cancellationToken);

        var desired = new Dictionary<(Guid Ancestor, Guid Descendant), int>();
        if (space.ParentSpaceId is { } parentId)
        {
            var newAncestors = await _context.SpacePaths
                .AsNoTracking()
                .Where(p => p.DescendantId == parentId)
                .Select(p => new { p.AncestorId, p.Depth })
                .ToListAsync(cancellationToken);

            foreach (var ancestor in newAncestors)
            {
                foreach (var (descendantId, below) in depthBelow)
                {
                    desired[(ancestor.AncestorId, descendantId)] = ancestor.Depth + 1 + below;
                }
            }
        }

        foreach (var row in external)
        {
            if (desired.Remove((row.AncestorId, row.DescendantId), out var depth))
            {
                if (row.Depth != depth)
                {
                    row.Rebase(depth);
                }
            }
            else
            {
                _context.SpacePaths.Remove(row);
            }
        }

        foreach (var ((ancestorId, descendantId), depth) in desired)
        {
            _context.SpacePaths.Add(new SpacePathRecord(space.AccountId, space.WorkspaceId, ancestorId, descendantId, depth));
        }
    }

    public Task<bool> IsActiveWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.Status == WorkspaceMemberStatus.Active, cancellationToken);

    public Task<bool> IsActiveTeamAsync(Guid workspaceId, Guid teamId, CancellationToken cancellationToken) =>
        _context.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == teamId && t.WorkspaceId == workspaceId && t.Status == TeamStatus.Active, cancellationToken);

    public async Task<IReadOnlyList<SpaceTreeRow>> ListTreeAsync(Guid workspaceId, IReadOnlyList<Guid> spaceIds, CancellationToken cancellationToken) =>
        await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && spaceIds.Contains(s.Id))
            .Select(s => new SpaceTreeRow(
                s.Id,
                s.ParentSpaceId,
                s.Name,
                s.Description,
                s.SpaceType,
                s.Visibility,
                s.Status,
                s.Position,
                s.Version,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SpaceMemberRow>> ListMembersAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken) =>
        await (
            from member in _context.SpaceMembers.AsNoTracking()
            where member.WorkspaceId == workspaceId && member.SpaceId == spaceId
            join team in _context.Teams.AsNoTracking() on member.SubjectId equals team.Id into teams
            from team in teams.DefaultIfEmpty()
            select new SpaceMemberRow(
                member.SubjectType,
                member.SubjectId,
                member.Role,
                member.SubjectType == SpaceMemberSubjectType.Team && team != null ? team.Name : null,
                member.CreatedAt))
            .ToListAsync(cancellationToken);

    public static long ComputeLockKey(Guid workspaceId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"workspaces.space-hierarchy:{workspaceId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
