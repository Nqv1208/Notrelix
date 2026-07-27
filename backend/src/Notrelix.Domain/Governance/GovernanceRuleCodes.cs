namespace Notrelix.Domain.Governance;

/// <summary>
/// Rule codes for the Governance bounded context.
/// </summary>
public static class GovernanceRuleCodes
{
    public const string Governance_Permission_CannotGrantHigherThanGranter = "Governance_Permission_CannotGrantHigherThanGranter";
    public const string Governance_Role_CannotRenameSystem = "Governance_Role_CannotRenameSystem";
    public const string Governance_Role_CannotDeleteSystem = "Governance_Role_CannotDeleteSystem";
    public const string Governance_Role_PermissionAlreadyAssigned = "Governance_Role_PermissionAlreadyAssigned";
    public const string Governance_ShareLink_PublicMustHaveExpiry = "Governance_ShareLink_PublicMustHaveExpiry";

    // ── PermissionTemplate ─────────────────────────────────────────────
    public const string Governance_PermissionTemplate_EntriesRequired = "Governance_PermissionTemplate_EntriesRequired";
    public const string Governance_PermissionTemplate_DuplicateEntry = "Governance_PermissionTemplate_DuplicateEntry";
    public const string Governance_PermissionTemplate_CannotModifySystem = "Governance_PermissionTemplate_CannotModifySystem";
    public const string Governance_PermissionTemplate_CannotDeleteSystem = "Governance_PermissionTemplate_CannotDeleteSystem";
}
