namespace Notrelix.Application.Features.Workspaces.Public.SpaceSubtree;

/// <summary>
/// Workspaces-published Space hierarchy fact: a Space and every Space below it (WSP-021). Consumers that
/// scope a measure to a department ask for the subtree instead of reading Workspaces persistence. Visibility
/// is the consumer's concern (it filters with the Governance visible Space ids); this fact only describes
/// the hierarchy.
/// </summary>
public interface ISpaceSubtreeFacts
{
    /// <summary>
    /// The Space and its non-deleted descendants (any status), or null when the Space does not exist in the
    /// Workspace or is deleted.
    /// </summary>
    Task<IReadOnlyList<Guid>?> GetSubtreeAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}
