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

    // ── CustomRole (owner decision 2026-10-31) ─────────────────────────
    public const string Governance_Role_NameRequired = "Governance_Role_NameRequired";
    public const string Governance_Role_NameTooLong = "Governance_Role_NameTooLong";
    public const string Governance_Role_NoChanges = "Governance_Role_NoChanges";
    public const string Governance_Role_ActionConflict = "Governance_Role_ActionConflict";
    public const string Governance_Role_Archived = "Governance_Role_Archived";
    public const string Governance_Role_BaseInvalid = "Governance_Role_BaseInvalid";
    public const string Governance_Role_ActionUnknown = "Governance_Role_ActionUnknown";
    public const string Governance_Role_ActionLocked = "Governance_Role_ActionLocked";
    public const string Governance_Role_ActionNotAddable = "Governance_Role_ActionNotAddable";
    public const string Governance_Role_ActionNotRemovable = "Governance_Role_ActionNotRemovable";
    public const string Governance_Role_ActionInBase = "Governance_Role_ActionInBase";
    public const string Governance_Role_ActionNotInBase = "Governance_Role_ActionNotInBase";
    public const string Governance_Role_GuestCannotAdd = "Governance_Role_GuestCannotAdd";
    public const string Governance_Role_OwnerCannotHoldRole = "Governance_Role_OwnerCannotHoldRole";
    public const string Governance_Role_ArchiveNeedsTarget = "Governance_Role_ArchiveNeedsTarget";
    public const string Governance_Role_ArchiveTargetInvalid = "Governance_Role_ArchiveTargetInvalid";
    public const string Governance_Role_NoSeat = "Governance_Role_NoSeat";
    public const string Governance_Role_LimitReached = "Governance_Role_LimitReached";
    public const string Governance_ShareLink_PublicMustHaveExpiry = "Governance_ShareLink_PublicMustHaveExpiry";

    // ── ResourcePermission ─────────────────────────────────────────────
    public const string Governance_ResourcePermission_SubjectTypeNotSupported = "Governance_ResourcePermission_SubjectTypeNotSupported";
    public const string Governance_ResourcePermission_ViewerRoleCapsGrant = "Governance_ResourcePermission_ViewerRoleCapsGrant";

    // ── ResourceAccessChange ───────────────────────────────────────────
    public const string Governance_AccessChange_SubjectRequired = "Governance_AccessChange_SubjectRequired";

    // ── PermissionTemplate ─────────────────────────────────────────────
    public const string Governance_PermissionTemplate_EntriesRequired = "Governance_PermissionTemplate_EntriesRequired";
    public const string Governance_PermissionTemplate_DuplicateEntry = "Governance_PermissionTemplate_DuplicateEntry";
    public const string Governance_PermissionTemplate_CannotModifySystem = "Governance_PermissionTemplate_CannotModifySystem";
    public const string Governance_PermissionTemplate_CannotDeleteSystem = "Governance_PermissionTemplate_CannotDeleteSystem";
}
