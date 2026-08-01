namespace Notrelix.Domain.Accounts;

/// <summary>
/// Rule codes for the Accounts bounded context.
/// </summary>
public static class AccountRuleCodes
{
    // ── Account ───────────────────────────────────────────────────────────
    public const string Accounts_Account_CannotRenameClosed = "Accounts_Account_CannotRenameClosed";

    // ── Domain ────────────────────────────────────────────────────────────
    public const string Accounts_Domain_CannotEnableAutoJoinUnverified = "Accounts_Domain_CannotEnableAutoJoinUnverified";

    // ── IdentityProvider ──────────────────────────────────────────────────
    public const string Accounts_IdentityProvider_InvalidProviderType = "Accounts_IdentityProvider_InvalidProviderType";

    // ── WorkspaceRoute ────────────────────────────────────────────────────
    public const string Accounts_WorkspaceRoute_InvalidWorkspaceId = "Accounts_WorkspaceRoute_InvalidWorkspaceId";

    // ── Invitation ────────────────────────────────────────────────────────
    public const string Accounts_Invitation_ExpiryMustBePositive = "Accounts_Invitation_ExpiryMustBePositive";
    public const string Accounts_Invitation_NotPending = "Accounts_Invitation_NotPending";
    public const string Accounts_Invitation_HasExpired = "Accounts_Invitation_HasExpired";

    // ── Owner ─────────────────────────────────────────────────────────────
    public const string Accounts_Owner_CannotDowngradeLastOwner = "Accounts_Owner_CannotDowngradeLastOwner";
    public const string Accounts_Owner_CannotSuspendLastOwner = "Accounts_Owner_CannotSuspendLastOwner";
    public const string Accounts_Owner_CannotRemoveLastOwner = "Accounts_Owner_CannotRemoveLastOwner";

    // ── Member ────────────────────────────────────────────────────────────
    public const string Accounts_Member_CannotChangeRoleOfInactive = "Accounts_Member_CannotChangeRoleOfInactive";
    public const string Accounts_Member_CannotActivateRemoved = "Accounts_Member_CannotActivateRemoved";
}
