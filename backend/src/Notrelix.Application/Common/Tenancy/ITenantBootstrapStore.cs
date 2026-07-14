namespace Notrelix.Application.Common.Tenancy;

public interface ITenantBootstrapStore
{
    Task<WorkspaceAccessSnapshot> ResolveWorkspaceAccessAsync(Guid workspaceId, Guid actorUserId, CancellationToken ct);

    /// <summary>
    /// Verifies that the user is an active AccountMember of the given account.
    /// Throws ForbiddenException if the user does not have access.
    /// This is the correct way to verify account access.
    /// AccountMember must NOT be used to resolve/select the current account.
    /// </summary>
    Task VerifyAccountAccessAsync(Guid accountId, Guid userId, CancellationToken ct);
}
