namespace Notrelix.Application.Features.Governance.Public.Visibility;

/// <summary>
/// Governance-published collection visibility (docs/product/governance.md
/// GOV-019 / GOV-029). Returns the ids a principal may see in one Workspace,
/// decided in the datastore by the same rules the access engine applies to a
/// single read of each resource (<c>ViewBoard</c> for a Board, <c>ViewPage</c>
/// for a Page, Space visibility for a Space). Collection handlers filter their
/// own queries with these ids; they never load everything and hide rows in
/// memory, and they never re-derive permission semantics.
/// </summary>
/// <remarks>
/// Only active resources are returned (not archived, not deleted) unless a member
/// says otherwise. A principal without an active membership in the Workspace sees nothing.
/// </remarks>
public interface IResourceVisibility
{
    Task<IReadOnlyList<Guid>> VisibleBoardIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// The archived (not deleted) boards the principal may see: the same rules as
    /// <see cref="VisibleBoardIdsAsync"/> applied to archived boards, because an archived
    /// board is read by the same decision as an active one (GOV-025 step 1).
    /// </summary>
    Task<IReadOnlyList<Guid>> VisibleArchivedBoardIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> VisiblePageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// The archived (trashed, not deleted) pages the principal may see: the same
    /// rules as <see cref="VisiblePageIdsAsync"/> applied to pages in the trash.
    /// </summary>
    Task<IReadOnlyList<Guid>> VisibleTrashedPageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Non-deleted Spaces (active and archived) the principal may see.</summary>
    Task<IReadOnlyList<Guid>> VisibleSpaceIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// The visible Spaces with the principal's effective level on each
    /// (GOV-027): <c>Viewer</c>, <c>Editor</c> or <c>Manager</c> (Manager = may
    /// manage the Space). Derived capability state for clients; the server
    /// still authorizes every operation.
    /// </summary>
    Task<IReadOnlyList<SpaceAccessEntry>> VisibleSpacesAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}

public sealed record SpaceAccessEntry(Guid SpaceId, string Level);
