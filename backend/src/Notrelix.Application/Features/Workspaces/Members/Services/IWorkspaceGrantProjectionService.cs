namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>
/// Workspaces-owned grant projection seam. Keeps the authz.access_grants
/// projection synchronized with Workspace membership changes inside the same
/// transaction as the membership mutation. Role vocabulary belongs to the
/// Workspaces context — Common never carries WorkspaceRole.
/// </summary>
public interface IWorkspaceGrantProjectionService
{
    Task SyncWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        DateTimeOffset now,
        CancellationToken ct);

    Task RevokeWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct);

    /// <summary>
    /// Revokes the grant of a member who ends their own participation. The member's workspace grant is
    /// also what lets that same person write the membership row, so the pending membership change is
    /// persisted (flushed inside the request transaction, not committed) before the grant is revoked;
    /// revoking first would make the membership update invisible to the person leaving.
    /// </summary>
    Task RevokeLeavingMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct);
}
