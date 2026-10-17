namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>
/// Serializes owner-affecting workspace membership operations (remove, demote,
/// suspend) by taking an exclusive <c>SELECT ... FOR UPDATE</c> row lock on the
/// target workspace for the duration of the owning request transaction.
///
/// Implemented in Infrastructure (PostgreSQL FOR UPDATE), mirroring
/// <see cref="Notrelix.Application.Features.Identity.Verification.Abstractions.IActiveVerificationTokenLocker"/>.
/// The lock guarantees that the active-owner count read later in the same
/// transaction is not stale relative to a concurrently committing
/// owner-affecting operation, so the Domain last-owner rule fails closed instead
/// of silently orphaning a workspace.
/// </summary>
public interface IWorkspaceOwnerUpdateLocker
{
    /// <summary>
    /// Returns the active (not archived, not soft-deleted) workspace while holding
    /// an exclusive row lock on it until the current request transaction commits or
    /// rolls back. Returns <c>null</c> when no such workspace exists.
    /// </summary>
    Task<Workspace?> LockWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);
}