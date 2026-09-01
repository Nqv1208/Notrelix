namespace Notrelix.Application.Features.Accounts.Public.Commands;

/// <summary>
/// Producer-owned public target action for Accounts membership mutation.
/// Owning context: Accounts — callers request the mutation; Accounts decides
/// and persists it. Exceptions:
/// <list type="bullet">
/// <item>unknown account or non-admissible lifecycle state</item>
/// <item>existing membership that is not active (cannot be provisioned)</item>
/// </list>
/// An already-active membership is a semantic no-op (idempotent re-invite).
///
/// BOUND-TX-002 (reviewed exception): this action currently executes inside the
/// caller's request transaction so that Account membership and Workspace
/// membership become visible atomically. Workflow owner: Workspaces
/// (AcceptInvitation); Accounts owns the Account-side mutation only.
/// Extraction blocker: the seam cannot become remote while this exception
/// stands. Removal trigger: Accounts/Workspaces service extraction or an
/// approved product decision allowing partial success/reconciliation.
/// </summary>
public interface IAccountMembershipActions
{
    /// <summary>
    /// Ensures the invited user holds an active Account membership in the
    /// target account. Expressed in Accounts language: the workspace
    /// invitation flow is the canonical caller, the mutation authority
    /// remains Accounts.
    /// </summary>
    Task EnsureWorkspaceInviteeMembershipAsync(
        Guid accountId,
        Guid userId,
        Guid invitedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
