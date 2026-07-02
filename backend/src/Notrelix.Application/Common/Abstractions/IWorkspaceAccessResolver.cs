namespace Notrelix.Application.Common.Abstractions;

/// <summary>
/// Resolves workspace access for a given actor.
/// Returns tenant identity (AccountId) and access status.
/// Implementation queries workspace data; handler does not know the source.
/// </summary>
public interface IWorkspaceAccessResolver
{
    Task<WorkspaceAccessSnapshot> ResolveAsync(Guid workspaceId, Guid actorUserId, CancellationToken ct);

    /// <summary>
    /// Resolves workspace by slug. Returns null if not found.
    /// Replaces direct Workspaces.FirstOrDefaultAsync(w => w.Slug == slug) from other bounded contexts.
    /// </summary>
    Task<WorkspaceBySlugSnapshot?> ResolveBySlugAsync(string slug, CancellationToken ct);
}

/// <summary>
/// Snapshot of workspace access state. Generic — does not contain business-specific permissions.
/// Use IPermissionEvaluator for specific permission checks (e.g., CanCreateBoard).
/// </summary>
public sealed record WorkspaceAccessSnapshot(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ActorUserId,
    bool CanAccess,
    bool IsWorkspaceActive);

/// <summary>
/// Snapshot of a workspace resolved by slug.
/// </summary>
public sealed record WorkspaceBySlugSnapshot(Guid Id, string Slug, Guid AccountId);