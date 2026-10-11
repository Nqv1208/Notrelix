using Notrelix.Application.Features.Documents.Public.SpacePlacement;
using Notrelix.Application.Features.WorkManagement.Public.SpacePlacement;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Composes the <see cref="SpaceContentFact"/> a Space archive/delete needs
/// (WSP-022): child Spaces from Workspaces' own store, Boards and Pages through
/// the producers' published placement facts.
/// </summary>
public interface ISpaceContentReader
{
    Task<SpaceContentFact> ReadAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}

public sealed class SpaceContentReader : ISpaceContentReader
{
    private readonly ISpaceStore _spaces;
    private readonly IBoardSpacePlacementFacts _boards;
    private readonly IPageSpacePlacementFacts _pages;

    public SpaceContentReader(ISpaceStore spaces, IBoardSpacePlacementFacts boards, IPageSpacePlacementFacts pages)
    {
        _spaces = spaces;
        _boards = boards;
        _pages = pages;
    }

    public async Task<SpaceContentFact> ReadAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var children = await _spaces.CountChildSpacesAsync(workspaceId, spaceId, cancellationToken);
        var boards = await _boards.CountInSpaceAsync(workspaceId, spaceId, cancellationToken);
        var pages = await _pages.CountInSpaceAsync(workspaceId, spaceId, cancellationToken);

        return new SpaceContentFact(
            children.Active,
            children.Existing,
            boards.Active + pages.Active,
            boards.Existing + pages.Existing);
    }
}
