namespace Notrelix.Domain.Workspaces;

/// <summary>
/// Rule codes for the Workspaces bounded context.
/// </summary>
public static class WorkspaceRuleCodes
{
    // ── Workspace ─────────────────────────────────────────────────────────
    public const string Workspaces_Workspace_NameTooLong = "Workspaces_Workspace_NameTooLong";
    public const string Workspaces_Workspace_CannotRenameArchived = "Workspaces_Workspace_CannotRenameArchived";
    public const string Workspaces_Workspace_CannotUpdateDescriptionArchived = "Workspaces_Workspace_CannotUpdateDescriptionArchived";
    public const string Workspaces_Workspace_CannotUpdateSettingsArchived = "Workspaces_Workspace_CannotUpdateSettingsArchived";
    public const string Workspaces_Workspace_CannotUnarchiveNonArchived = "Workspaces_Workspace_CannotUnarchiveNonArchived";

    // ── Invitation ────────────────────────────────────────────────────────
    public const string Workspaces_Invitation_ExpiryMustBePositive = "Workspaces_Invitation_ExpiryMustBePositive";
    public const string Workspaces_Invitation_NotPending = "Workspaces_Invitation_NotPending";
    public const string Workspaces_Invitation_HasExpired = "Workspaces_Invitation_HasExpired";
    public const string Workspaces_Invitation_PendingAlreadyExists = "Workspaces_Invitation_PendingAlreadyExists";
    public const string Workspaces_Invitation_CannotInviteAsOwner = "Workspaces_Invitation_CannotInviteAsOwner";
    public const string Workspaces_Invitation_CannotResendNonPendingExpired = "Workspaces_Invitation_CannotResendNonPendingExpired";

    // ── Member ────────────────────────────────────────────────────────────
    public const string Workspaces_Member_CannotChangeRoleOfInactive = "Workspaces_Member_CannotChangeRoleOfInactive";
    public const string Workspaces_Member_CannotPromoteInactiveToOwner = "Workspaces_Member_CannotPromoteInactiveToOwner";
    public const string Workspaces_Member_CannotActivateRemoved = "Workspaces_Member_CannotActivateRemoved";
    public const string Workspaces_Member_CannotActOnLastOwner = "Workspaces_Member_CannotActOnLastOwner";
    public const string Workspaces_Member_CannotDirectlyAssignOwner = "Workspaces_Member_CannotDirectlyAssignOwner";
    public const string Workspaces_Member_CannotSuspendRemoved = "Workspaces_Member_CannotSuspendRemoved";

    // ── OwnerRules ────────────────────────────────────────────────────────
    public const string Workspaces_Owner_CannotDowngradeLastOwner = "Workspaces_Owner_CannotDowngradeLastOwner";
    public const string Workspaces_Owner_CannotSuspendLastOwner = "Workspaces_Owner_CannotSuspendLastOwner";
    public const string Workspaces_Owner_CannotRemoveLastOwner = "Workspaces_Owner_CannotRemoveLastOwner";

    // ── Team ──────────────────────────────────────────────────────────────
    public const string Workspaces_Team_CannotRenameArchived = "Workspaces_Team_CannotRenameArchived";
    public const string Workspaces_Team_CannotUpdateDescriptionArchived = "Workspaces_Team_CannotUpdateDescriptionArchived";
    public const string Workspaces_Team_CannotAddMemberArchived = "Workspaces_Team_CannotAddMemberArchived";
    public const string Workspaces_Team_UserAlreadyMember = "Workspaces_Team_UserAlreadyMember";
    public const string Workspaces_Team_CannotRemoveMemberArchived = "Workspaces_Team_CannotRemoveMemberArchived";
    public const string Workspaces_Team_CannotChangeMemberRoleArchived = "Workspaces_Team_CannotChangeMemberRoleArchived";
    public const string Workspaces_Team_UserNotActiveMember = "Workspaces_Team_UserNotActiveMember";
    public const string Workspaces_Team_CannotUnarchiveNonArchived = "Workspaces_Team_CannotUnarchiveNonArchived";
    public const string Workspaces_Team_CannotRemoveLastLead = "Workspaces_Team_CannotRemoveLastLead";
    public const string Workspaces_Team_CannotDowngradeLastLead = "Workspaces_Team_CannotDowngradeLastLead";
    public const string Workspaces_Team_LastLeadCannotLeave = "Workspaces_Team_LastLeadCannotLeave";

    // ── TeamMember ────────────────────────────────────────────────────────
    public const string Workspaces_TeamMember_AlreadyActive = "Workspaces_TeamMember_AlreadyActive";
    public const string Workspaces_TeamMember_CannotChangeRoleOfInactive = "Workspaces_TeamMember_CannotChangeRoleOfInactive";

    // ── Space ─────────────────────────────────────────────────────────────
    public const string Workspaces_Space_CannotRenameArchived = "Workspaces_Space_CannotRenameArchived";
    public const string Workspaces_Space_CannotUpdateDescriptionArchived = "Workspaces_Space_CannotUpdateDescriptionArchived";
    public const string Workspaces_Space_CannotChangeVisibilityArchived = "Workspaces_Space_CannotChangeVisibilityArchived";
    public const string Workspaces_Space_CannotChangeTypeArchived = "Workspaces_Space_CannotChangeTypeArchived";
    public const string Workspaces_Space_CannotUnarchiveNonArchived = "Workspaces_Space_CannotUnarchiveNonArchived";

    // ── InvitationTokenHash ───────────────────────────────────────────────
    public const string Workspaces_InvitationTokenHash_InvalidFormat = "Workspaces_InvitationTokenHash_InvalidFormat";

    // ── WorkspaceSettings ─────────────────────────────────────────────────
    public const string Workspaces_Settings_DefaultMemberRoleMustBeGuestOrMember = "Workspaces_Settings_DefaultMemberRoleMustBeGuestOrMember";
    public const string Workspaces_Settings_InvitationExpiryDaysOutOfRange = "Workspaces_Settings_InvitationExpiryDaysOutOfRange";
}
