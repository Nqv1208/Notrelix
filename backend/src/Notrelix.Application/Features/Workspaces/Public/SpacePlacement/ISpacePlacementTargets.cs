namespace Notrelix.Application.Features.Workspaces.Public.SpacePlacement;

/// <summary>
/// Workspaces-published facts about a Space as a placement target for
/// resources owned by other contexts (Boards, Pages; docs/product/workspaces.md
/// §79). Work Management and Documents store the placement themselves and use
/// this contract to reject a missing, foreign or inactive Space; they never read
/// Workspaces persistence. Authorization of the placement remains the
/// Governance decision on the Space resource (GOV-028).
/// </summary>
public interface ISpacePlacementTargets
{
    /// <summary>The Space in the given Workspace (not deleted), or null.</summary>
    Task<SpacePlacementTarget?> FindAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}

/// <param name="IsActive">Not archived; only an active Space accepts new or moved resources.</param>
public sealed record SpacePlacementTarget(Guid SpaceId, Guid WorkspaceId, bool IsActive);
