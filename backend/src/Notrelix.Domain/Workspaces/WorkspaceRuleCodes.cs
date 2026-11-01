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
    public const string Workspaces_Workspace_SlugTooShort = "Workspaces_Workspace_SlugTooShort";
    public const string Workspaces_Workspace_SlugTooLong = "Workspaces_Workspace_SlugTooLong";
    public const string Workspaces_Workspace_SlugReserved = "Workspaces_Workspace_SlugReserved";

    // ── Invitation ────────────────────────────────────────────────────────
    public const string Workspaces_Invitation_ExpiryMustBePositive = "Workspaces_Invitation_ExpiryMustBePositive";
    public const string Workspaces_Invitation_NotPending = "Workspaces_Invitation_NotPending";
    public const string Workspaces_Invitation_HasExpired = "Workspaces_Invitation_HasExpired";
    public const string Workspaces_Invitation_PendingAlreadyExists = "Workspaces_Invitation_PendingAlreadyExists";
    public const string Workspaces_Invitation_CannotInviteAsOwner = "Workspaces_Invitation_CannotInviteAsOwner";
    public const string Workspaces_Invitation_CannotInviteAsAdmin = "Workspaces_Invitation_CannotInviteAsAdmin";
    public const string Workspaces_Invitation_ResendTooSoon = "Workspaces_Invitation_ResendTooSoon";
    public const string Workspaces_Invitation_CannotResendNonPendingExpired = "Workspaces_Invitation_CannotResendNonPendingExpired";
    public const string Workspaces_Invitation_InitialEntriesRequireMemberOrGuest = "Workspaces_Invitation_InitialEntriesRequireMemberOrGuest";
    public const string Workspaces_Invitation_InitialEntryInvalid = "Workspaces_Invitation_InitialEntryInvalid";
    public const string Workspaces_Invitation_GuestEntryRoleTooPrivileged = "Workspaces_Invitation_GuestEntryRoleTooPrivileged";
    public const string Workspaces_Invitation_InitialEntryDuplicate = "Workspaces_Invitation_InitialEntryDuplicate";
    public const string Workspaces_Invitation_TooManyInitialEntries = "Workspaces_Invitation_TooManyInitialEntries";
    public const string Workspaces_Invitation_ResourceKindNotInvitable = "Workspaces_Invitation_ResourceKindNotInvitable";
    public const string Workspaces_Invitation_ResourceLevelAboveViewer = "Workspaces_Invitation_ResourceLevelAboveViewer";
    public const string Workspaces_Invitation_ResourceInvitationMustBeGuest = "Workspaces_Invitation_ResourceInvitationMustBeGuest";

    // ── Member ────────────────────────────────────────────────────────────
    public const string Workspaces_Member_CannotChangeRoleOfInactive = "Workspaces_Member_CannotChangeRoleOfInactive";
    public const string Workspaces_Member_CannotPromoteInactiveToOwner = "Workspaces_Member_CannotPromoteInactiveToOwner";
    public const string Workspaces_Member_CannotActivateRemoved = "Workspaces_Member_CannotActivateRemoved";
    public const string Workspaces_Member_CannotRejoinNonRemoved = "Workspaces_Member_CannotRejoinNonRemoved";
    public const string Workspaces_Member_CannotActOnLastOwner = "Workspaces_Member_CannotActOnLastOwner";
    public const string Workspaces_Member_CannotDirectlyAssignOwner = "Workspaces_Member_CannotDirectlyAssignOwner";
    public const string Workspaces_Member_ActorCannotAssignRole = "Workspaces_Member_ActorCannotAssignRole";
    public const string Workspaces_Member_ActorCannotActOnMember = "Workspaces_Member_ActorCannotActOnMember";
    public const string Workspaces_Member_CannotActOnSelf = "Workspaces_Member_CannotActOnSelf";
    public const string Workspaces_Member_NotInAccount = "Workspaces_Member_NotInAccount";
    public const string Workspaces_Member_SeatUnavailable = "Workspaces_Member_SeatUnavailable";
    public const string Workspaces_Member_CannotSuspendRemoved = "Workspaces_Member_CannotSuspendRemoved";
    public const string Workspaces_Member_CannotEditRemovedProfile = "Workspaces_Member_CannotEditRemovedProfile";
    public const string Workspaces_Member_InvalidJobTitle = "Workspaces_Member_InvalidJobTitle";
    public const string Workspaces_Member_JobTitleTooLong = "Workspaces_Member_JobTitleTooLong";

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
    public const string Workspaces_Space_ParentInOtherWorkspace = "Workspaces_Space_ParentInOtherWorkspace";
    public const string Workspaces_Space_ParentNotActive = "Workspaces_Space_ParentNotActive";
    public const string Workspaces_Space_MoveCreatesCycle = "Workspaces_Space_MoveCreatesCycle";
    public const string Workspaces_Space_DuplicateSiblingName = "Workspaces_Space_DuplicateSiblingName";
    public const string Workspaces_Space_CannotMoveArchived = "Workspaces_Space_CannotMoveArchived";
    public const string Workspaces_Space_CannotArchiveWithActiveContent = "Workspaces_Space_CannotArchiveWithActiveContent";
    public const string Workspaces_Space_CannotDeleteWithContent = "Workspaces_Space_CannotDeleteWithContent";
    public const string Workspaces_Space_ParentMustBeActiveToReactivate = "Workspaces_Space_ParentMustBeActiveToReactivate";
    public const string Workspaces_Space_CannotChangeMembersArchived = "Workspaces_Space_CannotChangeMembersArchived";
    public const string Workspaces_Space_MemberAlreadyExists = "Workspaces_Space_MemberAlreadyExists";
    public const string Workspaces_Space_MemberNotFound = "Workspaces_Space_MemberNotFound";
    public const string Workspaces_Space_CannotRemoveLastAdmin = "Workspaces_Space_CannotRemoveLastAdmin";
    public const string Workspaces_Space_CannotDowngradeLastAdmin = "Workspaces_Space_CannotDowngradeLastAdmin";

    // ── InvitationTokenHash ───────────────────────────────────────────────
    public const string Workspaces_InvitationTokenHash_InvalidFormat = "Workspaces_InvitationTokenHash_InvalidFormat";

    // ── WorkspaceSettings ─────────────────────────────────────────────────
    public const string Workspaces_Settings_DefaultMemberRoleMustBeGuestOrMember = "Workspaces_Settings_DefaultMemberRoleMustBeGuestOrMember";
    public const string Workspaces_Settings_InvitationExpiryDaysOutOfRange = "Workspaces_Settings_InvitationExpiryDaysOutOfRange";
    public const string Workspaces_Settings_GuestDefaultExpiryDaysInvalid = "Workspaces_Settings_GuestDefaultExpiryDaysInvalid";
    public const string Workspaces_Settings_SizeInvalid = "Workspaces_Settings_SizeInvalid";
    public const string Workspaces_Settings_LanguageInvalid = "Workspaces_Settings_LanguageInvalid";
    public const string Workspaces_Settings_TimeZoneInvalid = "Workspaces_Settings_TimeZoneInvalid";
    public const string Workspaces_Settings_WeekStartInvalid = "Workspaces_Settings_WeekStartInvalid";

    // ── Navigation (recents and pins, WX-DEC-011) ─────────────────────────
    public const string Workspaces_Navigation_UnsupportedResourceKind = "Workspaces_Navigation_UnsupportedResourceKind";
    public const string Workspaces_Navigation_PinLimitReached = "Workspaces_Navigation_PinLimitReached";

    // ── Onboarding (draft and getting started) ────────────────────────────
    public const string Workspaces_Onboarding_DraftStepInvalid = "Workspaces_Onboarding_DraftStepInvalid";
    public const string Workspaces_Onboarding_DraftDataInvalid = "Workspaces_Onboarding_DraftDataInvalid";
    public const string Workspaces_Onboarding_DraftDataTooLarge = "Workspaces_Onboarding_DraftDataTooLarge";
}
