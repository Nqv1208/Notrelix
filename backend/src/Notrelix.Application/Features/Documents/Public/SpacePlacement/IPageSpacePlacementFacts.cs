namespace Notrelix.Application.Features.Documents.Public.SpacePlacement;

/// <summary>
/// Documents-published placement facts: how many Pages are placed in a
/// Workspaces Space. Workspaces consumes it to block archiving/deleting a Space
/// that still holds Pages (WSP-022); it never reads Documents persistence itself.
/// </summary>
public interface IPageSpacePlacementFacts
{
    Task<SpacePlacedPageCount> CountInSpaceAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}

/// <param name="Active">Not archived and not deleted.</param>
/// <param name="Existing">Not deleted (active or archived).</param>
public sealed record SpacePlacedPageCount(int Active, int Existing);
