namespace Notrelix.Application.Features.Workspaces.Public.Sharing;

/// <summary>
/// Workspaces-published facts about whether a Workspace may serve public links (public forms, share links).
/// Other contexts ask here instead of reading Workspaces persistence. A Workspace that is archived, deleted or
/// has switched public links off serves nothing to the public, whatever its resources say.
/// </summary>
public interface IWorkspaceSharingFacts
{
    /// <summary>The facts of a Workspace of the request's tenant, or null when it does not exist (or is deleted).</summary>
    Task<WorkspaceSharingFacts?> FindAsync(Guid workspaceId, CancellationToken cancellationToken);
}

/// <param name="IsActive">Not archived and not deleted.</param>
/// <param name="AllowsPublicLinks">The workspace-wide "public links" switch (off by default).</param>
public sealed record WorkspaceSharingFacts(bool IsActive, bool AllowsPublicLinks);
