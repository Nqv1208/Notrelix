using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// In-memory <see cref="ISpaceStore"/> for Space handler orchestration tests.
/// Ancestry is derived by walking <see cref="Space.ParentSpaceId"/>; the real
/// closure index, advisory lock and RLS are proven on PostgreSQL by the
/// org-tree integration suites, not here. Workspace members and Teams that
/// count as eligible Space subjects are stated explicitly per test.
/// </summary>
public sealed class InMemorySpaceStore : ISpaceStore
{
    public List<Space> Spaces { get; } = [];
    public HashSet<(Guid WorkspaceId, Guid UserId)> ActiveMembers { get; } = [];
    public HashSet<(Guid WorkspaceId, Guid TeamId)> ActiveTeams { get; } = [];
    public Dictionary<Guid, string> TeamNames { get; } = [];
    public int LockCount { get; private set; }
    public List<Guid> StagedCreated { get; } = [];
    public List<Guid> StagedMoved { get; } = [];

    public Task LockHierarchyAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        LockCount++;
        return Task.CompletedTask;
    }

    public Task<Space?> FindAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult(Spaces.FirstOrDefault(s => s.Id == spaceId && s.WorkspaceId == workspaceId && !s.IsDeleted));

    public void Add(Space space) => Spaces.Add(space);

    public Task<SpaceParentFact?> FindParentFactAsync(Guid parentSpaceId, CancellationToken cancellationToken)
    {
        var parent = Spaces.FirstOrDefault(s => s.Id == parentSpaceId && !s.IsDeleted);
        if (parent is null)
            return Task.FromResult<SpaceParentFact?>(null);

        var path = new List<Guid>();
        for (var current = parent; current is not null; current = Spaces.FirstOrDefault(s => s.Id == current.ParentSpaceId))
            path.Insert(0, current.Id);

        return Task.FromResult<SpaceParentFact?>(
            new SpaceParentFact(parent.Id, parent.WorkspaceId, parent.Status == SpaceStatus.Active, path));
    }

    public Task<IReadOnlyList<SpaceSiblingEntry>> LoadSiblingsAsync(Guid workspaceId, Guid? parentSpaceId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SpaceSiblingEntry>>(Spaces
            .Where(s => s.WorkspaceId == workspaceId && s.ParentSpaceId == parentSpaceId && !s.IsDeleted)
            .Select(s => new SpaceSiblingEntry(s.Id, s.Name, s.Position))
            .ToList());

    public Task<(int Active, int Existing)> CountChildSpacesAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var children = Spaces.Where(s => s.WorkspaceId == workspaceId && s.ParentSpaceId == spaceId && !s.IsDeleted).ToList();
        return Task.FromResult((children.Count(s => s.Status == SpaceStatus.Active), children.Count));
    }

    public Task StageCreatedAsync(Space space, CancellationToken cancellationToken)
    {
        StagedCreated.Add(space.Id);
        return Task.CompletedTask;
    }

    public Task StageMovedAsync(Space space, CancellationToken cancellationToken)
    {
        StagedMoved.Add(space.Id);
        return Task.CompletedTask;
    }

    public Task<bool> IsActiveWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(ActiveMembers.Contains((workspaceId, userId)));

    public Task<bool> IsActiveTeamAsync(Guid workspaceId, Guid teamId, CancellationToken cancellationToken) =>
        Task.FromResult(ActiveTeams.Contains((workspaceId, teamId)));

    public Task<IReadOnlyList<SpaceTreeRow>> ListTreeAsync(Guid workspaceId, IReadOnlyList<Guid> spaceIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SpaceTreeRow>>(Spaces
            .Where(s => s.WorkspaceId == workspaceId && spaceIds.Contains(s.Id) && !s.IsDeleted)
            .Select(s => new SpaceTreeRow(s.Id, s.ParentSpaceId, s.Name, s.Description, s.SpaceType, s.Visibility, s.Status, s.Position, s.Version, s.CreatedAt))
            .ToList());

    public Task<IReadOnlyList<SpaceMemberRow>> ListMembersAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SpaceMemberRow>>(Spaces
            .Where(s => s.WorkspaceId == workspaceId && s.Id == spaceId)
            .SelectMany(s => s.Members)
            .Select(m => new SpaceMemberRow(
                m.SubjectType,
                m.SubjectId,
                m.Role,
                m.SubjectType == SpaceMemberSubjectType.Team ? TeamNames.GetValueOrDefault(m.SubjectId) : null,
                m.CreatedAt))
            .ToList());
}
