namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

/// <param name="NewParentSpaceId">Target parent Space; null moves the Space (and its subtree) to the workspace root.</param>
/// <param name="PreviousSiblingId">Place after this sibling of the target parent (with <paramref name="NextSiblingId"/>: between the two). Both null appends.</param>
/// <param name="NextSiblingId">Place before this sibling of the target parent.</param>
/// <param name="ExpectedVersion">The Space version the client last read (SpaceTreeNode.version).</param>
public sealed record MoveSpaceRequest(
    Guid? NewParentSpaceId,
    Guid? PreviousSiblingId,
    Guid? NextSiblingId,
    long ExpectedVersion);
