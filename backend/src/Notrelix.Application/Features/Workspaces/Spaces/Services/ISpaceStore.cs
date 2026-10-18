using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Workspaces-owned persistence port for the Space hierarchy (WSP-021..WSP-024,
/// ADR-009). Writes are tracked on the request unit of work and saved by the
/// data session; the closure index <c>workspace.space_paths</c> is staged in the
/// same transaction as the Space write. Hierarchy reads used as Domain facts
/// must happen after <see cref="LockHierarchyAsync"/> so concurrent moves cannot
/// interleave (no cycle can be formed by two racing moves).
/// </summary>
public interface ISpaceStore
{
    /// <summary>Serializes hierarchy writes of one Workspace inside the request transaction.</summary>
    Task LockHierarchyAsync(Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>Tracked, non-deleted Space (with members) of the Workspace, or null.</summary>
    Task<Space?> FindAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);

    void Add(Space space);

    /// <summary>
    /// Parent fact: the non-deleted Space in the request tenant, whether it is
    /// active (not archived), and its ancestor path (root → parent, inclusive).
    /// Null when no such Space is visible to the request tenant (missing, deleted
    /// or in another Workspace).
    /// </summary>
    Task<SpaceParentFact?> FindParentFactAsync(Guid parentSpaceId, CancellationToken cancellationToken);

    /// <summary>Non-deleted Spaces directly under the parent (null = Workspace root).</summary>
    Task<IReadOnlyList<SpaceSiblingEntry>> LoadSiblingsAsync(Guid workspaceId, Guid? parentSpaceId, CancellationToken cancellationToken);

    /// <summary>Child Spaces of a Space: (active, existing = not deleted).</summary>
    Task<(int Active, int Existing)> CountChildSpacesAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Stages the closure rows of a newly created Space.</summary>
    Task StageCreatedAsync(Space space, CancellationToken cancellationToken);

    /// <summary>Re-points the closure rows of a moved Space's whole subtree to its new ancestors.</summary>
    Task StageMovedAsync(Space space, CancellationToken cancellationToken);

    Task<bool> IsActiveWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<bool> IsActiveTeamAsync(Guid workspaceId, Guid teamId, CancellationToken cancellationToken);

    /// <summary>Read-only tree rows for the given Space ids (non-deleted).</summary>
    Task<IReadOnlyList<SpaceTreeRow>> ListTreeAsync(Guid workspaceId, IReadOnlyList<Guid> spaceIds, CancellationToken cancellationToken);

    /// <summary>Member entries of a Space with the Team name when the subject is a Team.</summary>
    Task<IReadOnlyList<SpaceMemberRow>> ListMembersAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);
}

public sealed record SpaceSiblingEntry(Guid Id, string Name, FractionalIndex Position);

public sealed record SpaceTreeRow(
    Guid Id,
    Guid? ParentSpaceId,
    string Name,
    string? Description,
    SpaceType SpaceType,
    SpaceVisibility Visibility,
    SpaceStatus Status,
    FractionalIndex Position,
    long Version,
    DateTimeOffset CreatedAt);

public sealed record SpaceMemberRow(
    SpaceMemberSubjectType SubjectType,
    Guid SubjectId,
    SpaceRole Role,
    string? TeamName,
    DateTimeOffset AddedAt);
