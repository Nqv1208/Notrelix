namespace Notrelix.Application.Common.Tenancy;

public interface ITenantBootstrapStore
{
    Task<WorkspaceAccessSnapshot> ResolveWorkspaceAccessAsync(Guid workspaceId, Guid actorUserId, CancellationToken ct);
    Task<bool> HasAccountAccessAsync(Guid accountId, CancellationToken cancellationToken);
    Task<AccountContextSnapshot?> ResolveResourceContextAsync(Guid resourceId, string resourceType, CancellationToken ct);
}
