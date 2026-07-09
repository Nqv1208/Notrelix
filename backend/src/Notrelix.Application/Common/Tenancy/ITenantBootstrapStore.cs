namespace Notrelix.Application.Common.Tenancy;

public interface ITenantBootstrapStore
{
    Task<WorkspaceAccessSnapshot> ResolveWorkspaceAccessAsync(Guid workspaceId, Guid actorUserId, CancellationToken ct);
    Task<bool> HasAccountAccessAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the user's account for metadata-only IAccountRequest (no AccountId in request).
    /// Returns the account ID from the user's first workspace membership.
    /// </summary>
    Task<Guid> ResolveUserAccountAsync(Guid userId, CancellationToken ct);
}
