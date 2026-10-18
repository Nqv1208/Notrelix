namespace Notrelix.Application.Features.WorkManagement.Public.SpacePlacement;

/// <summary>
/// Work Management-published placement facts: how many Boards are placed in a
/// Workspaces Space. Workspaces consumes it to block archiving/deleting a Space
/// that still holds Boards (WSP-022); it never reads Work persistence itself.
/// </summary>
public interface IBoardSpacePlacementFacts
{
    Task<SpacePlacedBoardCount> CountInSpaceAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}

/// <param name="Active">Not archived and not deleted.</param>
/// <param name="Existing">Not deleted (active or archived).</param>
public sealed record SpacePlacedBoardCount(int Active, int Existing);
