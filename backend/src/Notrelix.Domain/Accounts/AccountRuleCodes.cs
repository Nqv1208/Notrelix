namespace Notrelix.Domain.Accounts;

/// <summary>
/// Rule codes for the Accounts bounded context.
/// </summary>
public static class AccountRuleCodes
{
    public const string Accounts_Account_CannotRenameClosed = "Accounts_Account_CannotRenameClosed";
    public const string Accounts_Domain_CannotEnableAutoJoinUnverified = "Accounts_Domain_CannotEnableAutoJoinUnverified";
    public const string Accounts_IdentityProvider_InvalidProviderType = "Accounts_IdentityProvider_InvalidProviderType";

    // ── WorkspaceRoute ─────────────────────────────────────────────────────
    public const string Accounts_WorkspaceRoute_InvalidWorkspaceId = "Accounts_WorkspaceRoute_InvalidWorkspaceId";
}
