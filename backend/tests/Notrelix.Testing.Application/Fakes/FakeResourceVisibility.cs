using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Test double for the Governance collection-visibility port. Visibility is
/// stated explicitly per test (no implicit "see everything" default), so a
/// handler test shows which ids the caller was allowed to see.
/// </summary>
public sealed class FakeResourceVisibility : IResourceVisibility
{
    public List<Guid> Boards { get; } = [];
    public List<Guid> Pages { get; } = [];
    public List<Guid> Spaces { get; } = [];

    public static FakeResourceVisibility WithBoards(params Guid[] boardIds)
    {
        var fake = new FakeResourceVisibility();
        fake.Boards.AddRange(boardIds);
        return fake;
    }

    public Task<IReadOnlyList<Guid>> VisibleBoardIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Boards.ToArray());

    public Task<IReadOnlyList<Guid>> VisiblePageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Pages.ToArray());

    public Task<IReadOnlyList<Guid>> VisibleSpaceIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Spaces.ToArray());
}
