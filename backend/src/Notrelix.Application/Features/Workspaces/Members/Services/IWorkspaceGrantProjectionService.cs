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

    /// <summary>
    /// A person who no longer holds an active grant in any Workspace of the Account loses their
    /// account-level Member grant in the same request transaction (derived access never outlives the
    /// membership that justified it). An account administrator's grant is never touched. Call after the
    /// Workspace grant has been revoked.
    /// </summary>
    Task RevokeAccountGrantWhenLastWorkspaceLeftAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct);

    /// <summary>
    /// Ends the custom-role assignments of a member of the Workspace. A member who is removed or
    /// reactivated returns to their built-in role and is configured again.
    /// </summary>
    Task ClearCustomRoleAssignmentsAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken ct);
}
