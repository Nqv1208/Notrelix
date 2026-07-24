namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Stable error codes for all business rule violations.
/// Every BusinessRuleException in the Domain layer must use one of these codes.
/// Pattern: Context_Aggregate_RuleName
/// Guard patterns: Guard_NullOrWhiteSpace, Guard_Empty, Guard_Negative, Guard_NotNegative
/// </summary>
public static class BusinessRuleCodes
{
    // ── SharedKernel / ValueObjects ───────────────────────────────────────
    public const string SharedKernel_Json_InvalidFormat = "SharedKernel_Json_InvalidFormat";
    public const string SharedKernel_Email_InvalidFormat = "SharedKernel_Email_InvalidFormat";
    public const string SharedKernel_Url_InvalidFormat = "SharedKernel_Url_InvalidFormat";
    public const string SharedKernel_Slug_InvalidFormat = "SharedKernel_Slug_InvalidFormat";
    public const string SharedKernel_Color_InvalidFormat = "SharedKernel_Color_InvalidFormat";
    public const string SharedKernel_Money_InvalidCurrency = "SharedKernel_Money_InvalidCurrency";
    public const string SharedKernel_DateRange_StartAfterEnd = "SharedKernel_DateRange_StartAfterEnd";

    // ── Guard (Common/Guard.cs) ───────────────────────────────────────────
    public const string Guard_Null = "Guard_Null";
    public const string Guard_NullOrWhiteSpace = "Guard_NullOrWhiteSpace";
    public const string Guard_Empty = "Guard_Empty";
    public const string Guard_Positive = "Guard_Positive";
    public const string Guard_NotNegative = "Guard_NotNegative";
    public const string Guard_MaxLength = "Guard_MaxLength";
    public const string Guard_InRange = "Guard_InRange";

    // ── Common / Lifecycle ────────────────────────────────────────────────
    public const string Common_EntityHasBeenDeleted = "Common_EntityHasBeenDeleted";
    public const string Common_Audit_InvalidTimestamp = "Common_Audit_InvalidTimestamp";
    public const string Common_Audit_CreatedAtAlreadySet = "Common_Audit_CreatedAtAlreadySet";
    public const string Common_Audit_UpdatedAtBeforeCreatedAt = "Common_Audit_UpdatedAtBeforeCreatedAt";

    // ── Common / Scope ────────────────────────────────────────────────────
    public const string Common_WorkspaceScopeMismatch = "Common_WorkspaceScopeMismatch";
    public const string Common_BoardScopeMismatch = "Common_BoardScopeMismatch";
    public const string Common_ChildNotFound = "Common_ChildNotFound";
    public const string Common_WidgetCoordinatesMustBeNonNegative = "Common_WidgetCoordinatesMustBeNonNegative";
    public const string Common_WidgetDimensionsMustBePositive = "Common_WidgetDimensionsMustBePositive";
    public const string Common_DefaultMemberRoleMustBeGuestOrMember = "Common_DefaultMemberRoleMustBeGuestOrMember";
    public const string Common_InvitationExpiryDaysOutOfRange = "Common_InvitationExpiryDaysOutOfRange";

    // ── Identity / Users ──────────────────────────────────────────────────
    public const string Identity_User_OAuthProviderAlreadyLinked = "Identity_User_OAuthProviderAlreadyLinked";
    public const string Identity_User_NoOAuthAccountForProvider = "Identity_User_NoOAuthAccountForProvider";

    // ── Identity / Sessions ───────────────────────────────────────────────
    public const string Identity_Session_ExpirationMustBeAfterCreation = "Identity_Session_ExpirationMustBeAfterCreation";
    public const string Identity_Session_CannotUpdateRefreshTokenOfInactive = "Identity_Session_CannotUpdateRefreshTokenOfInactive";
    public const string Identity_Session_CannotRevokeExpired = "Identity_Session_CannotRevokeExpired";
    public const string Identity_Session_CannotExpireRevoked = "Identity_Session_CannotExpireRevoked";

    // ── Identity / Tokens ─────────────────────────────────────────────────
    public const string Identity_ApiToken_InvalidScopesFormat = "Identity_ApiToken_InvalidScopesFormat";
    public const string Identity_ApiToken_CannotUseExpired = "Identity_ApiToken_CannotUseExpired";
    public const string Identity_ApiToken_CannotUseInactive = "Identity_ApiToken_CannotUseInactive";
    public const string Identity_OneTimeToken_HashVersionMustBePositive = "Identity_OneTimeToken_HashVersionMustBePositive";
    public const string Identity_OneTimeToken_ExpirationMustBeAfterCreation = "Identity_OneTimeToken_ExpirationMustBeAfterCreation";
    public const string Identity_OneTimeToken_AlreadyUsed = "Identity_OneTimeToken_AlreadyUsed";
    public const string Identity_OneTimeToken_CannotUseExpired = "Identity_OneTimeToken_CannotUseExpired";
    public const string Identity_OneTimeToken_CannotExpireUsed = "Identity_OneTimeToken_CannotExpireUsed";

    // ── Identity / Security ───────────────────────────────────────────────
    public const string Identity_LoginAttempt_MustHaveUserIdOrEmail = "Identity_LoginAttempt_MustHaveUserIdOrEmail";
    public const string Identity_LoginAttempt_SuccessfulCannotHaveReason = "Identity_LoginAttempt_SuccessfulCannotHaveReason";
    public const string Identity_LoginAttempt_FailedMustHaveReason = "Identity_LoginAttempt_FailedMustHaveReason";

    // ── Identity / MFA ────────────────────────────────────────────────────
    public const string Identity_Mfa_AuthenticatorRequiresSecret = "Identity_Mfa_AuthenticatorRequiresSecret";
    public const string Identity_Mfa_EmailSmsRequiresDestination = "Identity_Mfa_EmailSmsRequiresDestination";
    public const string Identity_Mfa_CannotVerifyDisabled = "Identity_Mfa_CannotVerifyDisabled";
    public const string Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive = "Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive";

    // ── Identity / Profiles ───────────────────────────────────────────────
    public const string Identity_Profile_InvalidPreferencesJson = "Identity_Profile_InvalidPreferencesJson";
    public const string Identity_Profile_InvalidTheme = "Identity_Profile_InvalidTheme";

    // ── Workspaces / Workspace ────────────────────────────────────────────
    public const string Workspaces_Workspace_NameTooLong = "Workspaces_Workspace_NameTooLong";
    public const string Workspaces_Workspace_CannotRenameArchived = "Workspaces_Workspace_CannotRenameArchived";
    public const string Workspaces_Workspace_CannotUpdateDescriptionArchived = "Workspaces_Workspace_CannotUpdateDescriptionArchived";
    public const string Workspaces_Workspace_CannotUpdateSettingsArchived = "Workspaces_Workspace_CannotUpdateSettingsArchived";
    public const string Workspaces_Workspace_CannotUnarchiveNonArchived = "Workspaces_Workspace_CannotUnarchiveNonArchived";

    // ── Workspaces / Invitation ───────────────────────────────────────────
    public const string Workspaces_Invitation_ExpiryMustBePositive = "Workspaces_Invitation_ExpiryMustBePositive";
    public const string Workspaces_Invitation_NotPending = "Workspaces_Invitation_NotPending";
    public const string Workspaces_Invitation_HasExpired = "Workspaces_Invitation_HasExpired";
    public const string Workspaces_Invitation_PendingAlreadyExists = "Workspaces_Invitation_PendingAlreadyExists";
    public const string Workspaces_Invitation_CannotInviteAsOwner = "Workspaces_Invitation_CannotInviteAsOwner";
    public const string Workspaces_Invitation_CannotResendNonPendingExpired = "Workspaces_Invitation_CannotResendNonPendingExpired";

    // ── Workspaces / Member ───────────────────────────────────────────────
    public const string Workspaces_Member_CannotChangeRoleOfInactive = "Workspaces_Member_CannotChangeRoleOfInactive";
    public const string Workspaces_Member_CannotPromoteInactiveToOwner = "Workspaces_Member_CannotPromoteInactiveToOwner";
    public const string Workspaces_Member_CannotActivateRemoved = "Workspaces_Member_CannotActivateRemoved";
    public const string Workspaces_Member_CannotActOnLastOwner = "Workspaces_Member_CannotActOnLastOwner";
    public const string Workspaces_Member_CannotDirectlyAssignOwner = "Workspaces_Member_CannotDirectlyAssignOwner";

    // ── Workspaces / OwnerRules ───────────────────────────────────────────
    public const string Workspaces_Owner_CannotDowngradeLastOwner = "Workspaces_Owner_CannotDowngradeLastOwner";
    public const string Workspaces_Owner_CannotSuspendLastOwner = "Workspaces_Owner_CannotSuspendLastOwner";
    public const string Workspaces_Owner_CannotRemoveLastOwner = "Workspaces_Owner_CannotRemoveLastOwner";

    // ── Workspaces / Team ─────────────────────────────────────────────────
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

    // ── Workspaces / TeamMember ───────────────────────────────────────────
    public const string Workspaces_TeamMember_AlreadyActive = "Workspaces_TeamMember_AlreadyActive";
    public const string Workspaces_TeamMember_CannotChangeRoleOfInactive = "Workspaces_TeamMember_CannotChangeRoleOfInactive";

    // ── Documents / Page ──────────────────────────────────────────────────
    public const string Documents_Page_CannotRenameArchived = "Documents_Page_CannotRenameArchived";
    public const string Documents_Page_CannotMoveArchived = "Documents_Page_CannotMoveArchived";
    public const string Documents_Page_CannotEditArchived = "Documents_Page_CannotEditArchived";

    // ── Documents / PageTree ──────────────────────────────────────────────
    public const string Documents_PageTree_CannotBeOwnParent = "Documents_PageTree_CannotBeOwnParent";
    public const string Documents_PageTree_MoveWouldCreateCycle = "Documents_PageTree_MoveWouldCreateCycle";

    // ── Documents / Block ─────────────────────────────────────────────────
    public const string Documents_Block_ContentCannotBeNull = "Documents_Block_ContentCannotBeNull";

    // ── Documents / BlockTree ─────────────────────────────────────────────
    public const string Documents_BlockTree_CannotBeOwnParent = "Documents_BlockTree_CannotBeOwnParent";
    public const string Documents_BlockTree_MoveWouldCreateCycle = "Documents_BlockTree_MoveWouldCreateCycle";
    public const string Documents_BlockTree_ParentNotFound = "Documents_BlockTree_ParentNotFound";
    public const string Documents_BlockTree_ParentMustBeInSamePage = "Documents_BlockTree_ParentMustBeInSamePage";

    // ── Documents / ResourceLink ──────────────────────────────────────────
    public const string Documents_ResourceLink_CannotCreateSelfReferencing = "Documents_ResourceLink_CannotCreateSelfReferencing";
    public const string Documents_ResourceLink_TargetMustBeInSameWorkspace = "Documents_ResourceLink_TargetMustBeInSameWorkspace";

    // ── Documents / PageTemplate ──────────────────────────────────────────
    public const string Documents_PageTemplate_CannotPublishArchived = "Documents_PageTemplate_CannotPublishArchived";

    // ── Collaboration / Notification ──────────────────────────────────────
    public const string Collaboration_Notification_CannotNotifySelf = "Collaboration_Notification_CannotNotifySelf";

    // ── Collaboration / Reaction ──────────────────────────────────────────
    public const string Collaboration_Reaction_DuplicateReaction = "Collaboration_Reaction_DuplicateReaction";

    // ── Collaboration / Comment ───────────────────────────────────────────
    public const string Collaboration_Comment_ParentNotFound = "Collaboration_Comment_ParentNotFound";
    public const string Collaboration_Comment_ParentMustBeInSameTarget = "Collaboration_Comment_ParentMustBeInSameTarget";

    // ── Collaboration / Attachment ────────────────────────────────────────
    public const string Collaboration_Attachment_FileSizeMustBePositive = "Collaboration_Attachment_FileSizeMustBePositive";
    public const string Collaboration_Attachment_MaxAttachmentsExceeded = "Collaboration_Attachment_MaxAttachmentsExceeded";
    public const string Collaboration_Attachment_FileSizeExceeded = "Collaboration_Attachment_FileSizeExceeded";

    // ── Billing / Plan ────────────────────────────────────────────────────
    public const string Billing_Plan_PriceCannotBeNegative = "Billing_Plan_PriceCannotBeNegative";
    public const string Billing_Plan_LimitCannotBeNegative = "Billing_Plan_LimitCannotBeNegative";

    // ── Billing / Subscription ────────────────────────────────────────────
    public const string Billing_Subscription_PeriodStartMustBeBeforeEnd = "Billing_Subscription_PeriodStartMustBeBeforeEnd";
    public const string Billing_Subscription_CannotChangePlanOfInactive = "Billing_Subscription_CannotChangePlanOfInactive";
    public const string Billing_Subscription_AlreadyInactive = "Billing_Subscription_AlreadyInactive";

    // ── Billing / Entitlement ─────────────────────────────────────────────
    public const string Billing_Entitlement_LimitCannotBeNegative = "Billing_Entitlement_LimitCannotBeNegative";
    public const string Billing_Entitlement_WorkspaceScopedRequiresTarget = "Billing_Entitlement_WorkspaceScopedRequiresTarget";
    public const string Billing_Entitlement_AccountScopedMustNotSpecifyTarget = "Billing_Entitlement_AccountScopedMustNotSpecifyTarget";
    public const string Billing_Entitlement_CannotChangeLimitOfNonActive = "Billing_Entitlement_CannotChangeLimitOfNonActive";
    public const string Billing_Entitlement_CannotDisableRevoked = "Billing_Entitlement_CannotDisableRevoked";
    public const string Billing_Entitlement_CannotExpireRevoked = "Billing_Entitlement_CannotExpireRevoked";
    public const string Billing_Entitlement_MustBeRestoredBeforeEnable = "Billing_Entitlement_MustBeRestoredBeforeEnable";
    public const string Billing_Entitlement_AlreadyRevoked = "Billing_Entitlement_AlreadyRevoked";

    // ── WorkManagement / Board ────────────────────────────────────────────
    public const string WorkManagement_Board_CannotRenameArchived = "WorkManagement_Board_CannotRenameArchived";
    public const string WorkManagement_Board_CannotUpdateDescriptionArchived = "WorkManagement_Board_CannotUpdateDescriptionArchived";
    public const string WorkManagement_Board_CannotUpdateBackgroundArchived = "WorkManagement_Board_CannotUpdateBackgroundArchived";
    public const string WorkManagement_Board_CannotChangeVisibilityArchived = "WorkManagement_Board_CannotChangeVisibilityArchived";
    public const string WorkManagement_Board_CannotGenerateIdentityArchived = "WorkManagement_Board_CannotGenerateIdentityArchived";
    public const string WorkManagement_Board_CannotDeleteDefaultGroup = "WorkManagement_Board_CannotDeleteDefaultGroup";

    // ── WorkManagement / Field ────────────────────────────────────────────
    public const string WorkManagement_Field_DoesNotBelongToBoard = "WorkManagement_Field_DoesNotBelongToBoard";
    public const string WorkManagement_Field_CannotUpdateDeleted = "WorkManagement_Field_CannotUpdateDeleted";
    public const string WorkManagement_Field_CannotUpdateSystem = "WorkManagement_Field_CannotUpdateSystem";
    public const string WorkManagement_Field_CannotDeleteSystem = "WorkManagement_Field_CannotDeleteSystem";
    public const string WorkManagement_Field_CannotWriteComputed = "WorkManagement_Field_CannotWriteComputed";
    public const string WorkManagement_Field_CannotAddOptionsForType = "WorkManagement_Field_CannotAddOptionsForType";
    public const string WorkManagement_Field_DuplicateOptionName = "WorkManagement_Field_DuplicateOptionName";
    public const string WorkManagement_Field_ReorderMustContainAllOptions = "WorkManagement_Field_ReorderMustContainAllOptions";
    public const string WorkManagement_Field_InvalidOptionValue = "WorkManagement_Field_InvalidOptionValue";
    public const string WorkManagement_Field_OptionNotFound = "WorkManagement_Field_OptionNotFound";

    // ── WorkManagement / FieldSettings ────────────────────────────────────
    public const string WorkManagement_FieldSettings_InvalidJsonFormat = "WorkManagement_FieldSettings_InvalidJsonFormat";

    // ── WorkManagement / FieldValue ───────────────────────────────────────
    public const string WorkManagement_FieldValue_CannotBeNull = "WorkManagement_FieldValue_CannotBeNull";
    public const string WorkManagement_FieldValue_InvalidJsonFormat = "WorkManagement_FieldValue_InvalidJsonFormat";
    public const string WorkManagement_FieldValue_InvalidStringValue = "WorkManagement_FieldValue_InvalidStringValue";
    public const string WorkManagement_FieldValue_InvalidStringFormat = "WorkManagement_FieldValue_InvalidStringFormat";
    public const string WorkManagement_FieldValue_TextExceedsMaxLength = "WorkManagement_FieldValue_TextExceedsMaxLength";
    public const string WorkManagement_FieldValue_NumberBelowMin = "WorkManagement_FieldValue_NumberBelowMin";
    public const string WorkManagement_FieldValue_NumberAboveMax = "WorkManagement_FieldValue_NumberAboveMax";
    public const string WorkManagement_FieldValue_InvalidBooleanValue = "WorkManagement_FieldValue_InvalidBooleanValue";
    public const string WorkManagement_FieldValue_InvalidSelectValue = "WorkManagement_FieldValue_InvalidSelectValue";
    public const string WorkManagement_FieldValue_InvalidMultiSelectValue = "WorkManagement_FieldValue_InvalidMultiSelectValue";
    public const string WorkManagement_FieldValue_InvalidDateValue = "WorkManagement_FieldValue_InvalidDateValue";

    // ── WorkManagement / Item ─────────────────────────────────────────────
    public const string WorkManagement_Item_ParentMustBelongToSameBoard = "WorkManagement_Item_ParentMustBelongToSameBoard";
    public const string WorkManagement_Item_DueDateMustBeAfterStartDate = "WorkManagement_Item_DueDateMustBeAfterStartDate";
    public const string WorkManagement_Item_CannotBeOwnParent = "WorkManagement_Item_CannotBeOwnParent";
    public const string WorkManagement_Item_ParentAssignmentWouldCreateCycle = "WorkManagement_Item_ParentAssignmentWouldCreateCycle";
    public const string WorkManagement_Item_CannotModifyArchived = "WorkManagement_Item_CannotModifyArchived";

    // ── WorkManagement / Dependency ───────────────────────────────────────
    public const string WorkManagement_Dependency_CannotDependOnSelf = "WorkManagement_Dependency_CannotDependOnSelf";
    public const string WorkManagement_Dependency_CannotCreateCycle = "WorkManagement_Dependency_CannotCreateCycle";

    // ── WorkManagement / View ─────────────────────────────────────────────
    public const string WorkManagement_View_KanbanMustUseKanbanConfig = "WorkManagement_View_KanbanMustUseKanbanConfig";
    public const string WorkManagement_View_TableMustUseTableConfig = "WorkManagement_View_TableMustUseTableConfig";
    public const string WorkManagement_View_CalendarMustUseCalendarConfig = "WorkManagement_View_CalendarMustUseCalendarConfig";
    public const string WorkManagement_View_TimelineMustUseTimelineConfig = "WorkManagement_View_TimelineMustUseTimelineConfig";
    public const string WorkManagement_View_CannotDeleteDefault = "WorkManagement_View_CannotDeleteDefault";

    // ── WorkManagement / Kanban ───────────────────────────────────────────
    public const string WorkManagement_Kanban_InvalidColumnField = "WorkManagement_Kanban_InvalidColumnField";
    public const string WorkManagement_Kanban_MustHaveVisibleField = "WorkManagement_Kanban_MustHaveVisibleField";
    public const string WorkManagement_Kanban_VisibleFieldIdsCannotBeEmpty = "WorkManagement_Kanban_VisibleFieldIdsCannotBeEmpty";
    public const string WorkManagement_Kanban_SwimlaneFieldIdCannotBeEmpty = "WorkManagement_Kanban_SwimlaneFieldIdCannotBeEmpty";

    // ── WorkManagement / Form ─────────────────────────────────────────────
    public const string WorkManagement_Form_CannotPublishClosed = "WorkManagement_Form_CannotPublishClosed";
    public const string WorkManagement_Form_CannotPublishNoQuestions = "WorkManagement_Form_CannotPublishNoQuestions";
    public const string WorkManagement_Form_CannotSubmitToDraft = "WorkManagement_Form_CannotSubmitToDraft";
    public const string WorkManagement_Form_CannotSubmitToClosed = "WorkManagement_Form_CannotSubmitToClosed";
    public const string WorkManagement_Form_CannotAddQuestionToClosed = "WorkManagement_Form_CannotAddQuestionToClosed";
    public const string WorkManagement_Form_DuplicateQuestionKey = "WorkManagement_Form_DuplicateQuestionKey";

    // ── WorkManagement / FormQuestion ─────────────────────────────────────
    public const string WorkManagement_FormQuestion_InvalidConfigJson = "WorkManagement_FormQuestion_InvalidConfigJson";

    // ── WorkManagement / FormSubmission ───────────────────────────────────
    public const string WorkManagement_FormSubmission_CannotRejectUnlessAccepted = "WorkManagement_FormSubmission_CannotRejectUnlessAccepted";
    public const string WorkManagement_FormSubmission_CannotMarkSpamUnlessAccepted = "WorkManagement_FormSubmission_CannotMarkSpamUnlessAccepted";
    public const string WorkManagement_FormSubmission_CannotProcessUnlessAccepted = "WorkManagement_FormSubmission_CannotProcessUnlessAccepted";
    public const string WorkManagement_FormSubmission_AlreadyDeleted = "WorkManagement_FormSubmission_AlreadyDeleted";

    // ── WorkManagement / Approval ─────────────────────────────────────────
    public const string WorkManagement_Approval_StepNotFound = "WorkManagement_Approval_StepNotFound";
    public const string WorkManagement_Approval_CannotAddStepsNonPending = "WorkManagement_Approval_CannotAddStepsNonPending";
    public const string WorkManagement_Approval_CannotApproveUnlessPending = "WorkManagement_Approval_CannotApproveUnlessPending";
    public const string WorkManagement_Approval_CannotRejectUnlessPending = "WorkManagement_Approval_CannotRejectUnlessPending";
    public const string WorkManagement_Approval_CannotCancelUnlessPending = "WorkManagement_Approval_CannotCancelUnlessPending";
    public const string WorkManagement_Approval_Step_CannotApproveUnlessPending = "WorkManagement_Approval_Step_CannotApproveUnlessPending";
    public const string WorkManagement_Approval_Step_CannotRejectUnlessPending = "WorkManagement_Approval_Step_CannotRejectUnlessPending";
    public const string WorkManagement_Approval_StepPositionInvalid = "WorkManagement_Approval_StepPositionInvalid";
    public const string WorkManagement_Approval_StepRequiresApprover = "WorkManagement_Approval_StepRequiresApprover";
    public const string WorkManagement_Approval_DecisionTimeRequired = "WorkManagement_Approval_DecisionTimeRequired";
    public const string WorkManagement_Approval_DuplicateStepPosition = "WorkManagement_Approval_DuplicateStepPosition";
    public const string WorkManagement_Approval_DuplicateApprover = "WorkManagement_Approval_DuplicateApprover";

    // ── WorkManagement / Template ─────────────────────────────────────────
    public const string WorkManagement_BoardTemplate_CannotDraftArchived = "WorkManagement_BoardTemplate_CannotDraftArchived";
    public const string WorkManagement_BoardTemplate_CannotPublishArchived = "WorkManagement_BoardTemplate_CannotPublishArchived";
    public const string WorkManagement_BoardTemplate_CanOnlyRestoreArchived = "WorkManagement_BoardTemplate_CanOnlyRestoreArchived";

    // ── WorkManagement / TimeTracking ─────────────────────────────────────
    public const string WorkManagement_TimeTracking_CannotStopNotRunning = "WorkManagement_TimeTracking_CannotStopNotRunning";
    public const string WorkManagement_TimeTracking_EndTimeMustBeAfterStart = "WorkManagement_TimeTracking_EndTimeMustBeAfterStart";

    // ── WorkManagement / Checklist ────────────────────────────────────────
    public const string WorkManagement_Checklist_ItemNotFound = "WorkManagement_Checklist_ItemNotFound";

    // ── Automation / Rule ─────────────────────────────────────────────────
    public const string Automation_Rule_NameCannotBeEmpty = "Automation_Rule_NameCannotBeEmpty";
    public const string Automation_Rule_MustHaveConfiguration = "Automation_Rule_MustHaveConfiguration";
    public const string Automation_Rule_MustHaveTrigger = "Automation_Rule_MustHaveTrigger";
    public const string Automation_Rule_MustHaveAction = "Automation_Rule_MustHaveAction";

    // ── Automation / Execution ────────────────────────────────────────────
    public const string Automation_Execution_CannotStartUnlessQueued = "Automation_Execution_CannotStartUnlessQueued";
    public const string Automation_Execution_CannotSucceedUnlessRunning = "Automation_Execution_CannotSucceedUnlessRunning";
    public const string Automation_Execution_CannotFailUnlessRunning = "Automation_Execution_CannotFailUnlessRunning";
    public const string Automation_Execution_ErrorRequiredOnFail = "Automation_Execution_ErrorRequiredOnFail";
    public const string Automation_Execution_CannotCancelUnlessQueuedOrRunning = "Automation_Execution_CannotCancelUnlessQueuedOrRunning";

    // ── Automation / Agent ────────────────────────────────────────────────
    public const string Automation_Agent_InvalidStatusTransition = "Automation_Agent_InvalidStatusTransition";

    // ── Automation / ScheduledJob ─────────────────────────────────────────
    public const string Automation_ScheduledJob_CannotCompleteFromStatus = "Automation_ScheduledJob_CannotCompleteFromStatus";
    public const string Automation_ScheduledJob_CannotFailFromStatus = "Automation_ScheduledJob_CannotFailFromStatus";

    // ── Integrations / WebhookDelivery ────────────────────────────────────
    public const string Integrations_WebhookDelivery_CannotScheduleRetryUnlessFailed = "Integrations_WebhookDelivery_CannotScheduleRetryUnlessFailed";
    public const string Integrations_Webhook_MaxRetriesOutOfRange = "Integrations_Webhook_MaxRetriesOutOfRange";

    // ── Analytics / Dashboard ─────────────────────────────────────────────
    public const string Analytics_Dashboard_WidgetNotFound = "Analytics_Dashboard_WidgetNotFound";
    public const string Analytics_Dashboard_InvalidWidgetConfig = "Analytics_Dashboard_InvalidWidgetConfig";

    // ── Billing / Usage ───────────────────────────────────────────────────
    public const string Billing_Usage_ValueCannotBeNegative = "Billing_Usage_ValueCannotBeNegative";
    public const string Billing_Usage_StartMustBeBeforeEnd = "Billing_Usage_StartMustBeBeforeEnd";
    public const string Billing_Usage_LimitExceeded = "Billing_Usage_LimitExceeded";
    public const string Billing_Usage_FeatureLimitExceeded = "Billing_Usage_FeatureLimitExceeded";

    // ── Integrations / Connection ─────────────────────────────────────────
    public const string Integrations_Connection_SecretVersionAlreadyExists = "Integrations_Connection_SecretVersionAlreadyExists";

    // ── Integrations / Calendar ───────────────────────────────────────────
    public const string Integrations_Calendar_ConnectionMustBeActive = "Integrations_Calendar_ConnectionMustBeActive";
    public const string Integrations_Calendar_CannotLinkEventToSelf = "Integrations_Calendar_CannotLinkEventToSelf";
    public const string Integrations_Calendar_CannotChangeDirectionDeactivated = "Integrations_Calendar_CannotChangeDirectionDeactivated";
    public const string Integrations_Calendar_CannotLinkEventsDeactivated = "Integrations_Calendar_CannotLinkEventsDeactivated";
    public const string Integrations_Calendar_EventLinkAlreadyExists = "Integrations_Calendar_EventLinkAlreadyExists";
    public const string Integrations_Calendar_EventLinkNotFound = "Integrations_Calendar_EventLinkNotFound";

    // ── Integrations / Connection ─────────────────────────────────────────
    public const string Integrations_Connection_AlreadyActive = "Integrations_Connection_AlreadyActive";
    public const string Integrations_Connection_ExpirationMustBeFuture = "Integrations_Connection_ExpirationMustBeFuture";

    // ── Accounts ─────────────────────────────────────────────────────────
    public const string Accounts_Account_CannotRenameClosed = "Accounts_Account_CannotRenameClosed";
    public const string Accounts_Domain_CannotEnableAutoJoinUnverified = "Accounts_Domain_CannotEnableAutoJoinUnverified";
    public const string Accounts_IdentityProvider_InvalidProviderType = "Accounts_IdentityProvider_InvalidProviderType";

    // ── Governance ───────────────────────────────────────────────────────
    public const string Governance_Permission_CannotGrantHigherThanGranter = "Governance_Permission_CannotGrantHigherThanGranter";
    public const string Governance_Role_CannotRenameSystem = "Governance_Role_CannotRenameSystem";
    public const string Governance_Role_CannotDeleteSystem = "Governance_Role_CannotDeleteSystem";
    public const string Governance_Role_PermissionAlreadyAssigned = "Governance_Role_PermissionAlreadyAssigned";
    public const string Governance_ShareLink_PublicMustHaveExpiry = "Governance_ShareLink_PublicMustHaveExpiry";

    // ── Workspaces / Space ───────────────────────────────────────────────
    public const string Workspaces_Space_CannotRenameArchived = "Workspaces_Space_CannotRenameArchived";
    public const string Workspaces_Space_CannotUpdateDescriptionArchived = "Workspaces_Space_CannotUpdateDescriptionArchived";
    public const string Workspaces_Space_CannotChangeVisibilityArchived = "Workspaces_Space_CannotChangeVisibilityArchived";
    public const string Workspaces_Space_CannotChangeTypeArchived = "Workspaces_Space_CannotChangeTypeArchived";
    public const string Workspaces_Space_CannotUnarchiveNonArchived = "Workspaces_Space_CannotUnarchiveNonArchived";

    // ── Workspaces / InvitationTokenHash ──────────────────────────────────
    public const string Workspaces_InvitationTokenHash_InvalidFormat = "Workspaces_InvitationTokenHash_InvalidFormat";

    // ── Billing / Plan ────────────────────────────────────────────────────
    public const string Billing_Plan_FeatureAlreadyAdded = "Billing_Plan_FeatureAlreadyAdded";

    // ── Billing / Subscription ────────────────────────────────────────────
    public const string Billing_Subscription_RenewalPeriodStartMustBeBeforeEnd = "Billing_Subscription_RenewalPeriodStartMustBeBeforeEnd";

    // ── Billing / Invoice ─────────────────────────────────────────────────
    public const string Billing_Invoice_CannotIssueUnlessDraft = "Billing_Invoice_CannotIssueUnlessDraft";
    public const string Billing_Invoice_CannotMarkVoidAsPaid = "Billing_Invoice_CannotMarkVoidAsPaid";
    public const string Billing_Invoice_CannotFailPaid = "Billing_Invoice_CannotFailPaid";
    public const string Billing_Invoice_CannotFailVoid = "Billing_Invoice_CannotFailVoid";
    public const string Billing_Invoice_CannotVoidPaid = "Billing_Invoice_CannotVoidPaid";

    // ── Billing / Usage ───────────────────────────────────────────────────
    public const string Billing_Usage_CurrentCannotBeNegative = "Billing_Usage_CurrentCannotBeNegative";
    public const string Billing_Usage_HardLimitCannotBeNegative = "Billing_Usage_HardLimitCannotBeNegative";
    public const string Billing_Usage_SoftLimitCannotBeNegative = "Billing_Usage_SoftLimitCannotBeNegative";
    public const string Billing_Usage_SoftLimitCannotExceedHard = "Billing_Usage_SoftLimitCannotExceedHard";
    public const string Billing_Usage_ExceedsHardLimitNoOverage = "Billing_Usage_ExceedsHardLimitNoOverage";
    public const string Billing_Usage_ConsumeAmountMustBePositive = "Billing_Usage_ConsumeAmountMustBePositive";
    public const string Billing_Usage_ReleaseAmountMustBePositive = "Billing_Usage_ReleaseAmountMustBePositive";
    public const string Billing_Usage_CannotReleaseBelowZero = "Billing_Usage_CannotReleaseBelowZero";

    // ── WorkManagement / FieldSettings ────────────────────────────────────
    public const string WorkManagement_FieldSettings_NumberMinMustBeNumber = "WorkManagement_FieldSettings_NumberMinMustBeNumber";
    public const string WorkManagement_FieldSettings_NumberMaxMustBeNumber = "WorkManagement_FieldSettings_NumberMaxMustBeNumber";
    public const string WorkManagement_FieldSettings_TextMaxLengthMustBeNumber = "WorkManagement_FieldSettings_TextMaxLengthMustBeNumber";
    public const string WorkManagement_FieldSettings_DateIncludeTimeMustBeBoolean = "WorkManagement_FieldSettings_DateIncludeTimeMustBeBoolean";
    public const string WorkManagement_FieldSettings_StatusMustIncludeTransitions = "WorkManagement_FieldSettings_StatusMustIncludeTransitions";

    // ── WorkManagement / Field ────────────────────────────────────────────
    public const string WorkManagement_Field_BelongsToDifferentWorkspace = "WorkManagement_Field_BelongsToDifferentWorkspace";

    // ── WorkManagement / View ─────────────────────────────────────────────
    public const string WorkManagement_View_DuplicateFilterRules = "WorkManagement_View_DuplicateFilterRules";
    public const string WorkManagement_View_DuplicateSortRules = "WorkManagement_View_DuplicateSortRules";

    // ── WorkManagement / Relation ─────────────────────────────────────────
    public const string WorkManagement_Relation_SourceAndTargetMustBeDifferent = "WorkManagement_Relation_SourceAndTargetMustBeDifferent";
    public const string WorkManagement_Relation_CannotCreateSelfReferencing = "WorkManagement_Relation_CannotCreateSelfReferencing";
    public const string WorkManagement_Relation_CannotResumeBroken = "WorkManagement_Relation_CannotResumeBroken";

    // ── WorkManagement / Connection ───────────────────────────────────────
    public const string WorkManagement_Connection_CannotConnectToSelf = "WorkManagement_Connection_CannotConnectToSelf";

    // ── WorkManagement / FormQuestion ─────────────────────────────────────
    public const string WorkManagement_FormQuestion_MaxLengthInvalidForType = "WorkManagement_FormQuestion_MaxLengthInvalidForType";
    public const string WorkManagement_FormQuestion_MinMaxInvalidForType = "WorkManagement_FormQuestion_MinMaxInvalidForType";
    public const string WorkManagement_FormQuestion_MaxFileSizeInvalidForType = "WorkManagement_FormQuestion_MaxFileSizeInvalidForType";
    public const string WorkManagement_FormQuestion_MaxLengthMustBePositive = "WorkManagement_FormQuestion_MaxLengthMustBePositive";
    public const string WorkManagement_FormQuestion_MaxFileSizeMustBePositive = "WorkManagement_FormQuestion_MaxFileSizeMustBePositive";
    public const string WorkManagement_FormQuestion_MinCannotExceedMax = "WorkManagement_FormQuestion_MinCannotExceedMax";

    // ── WorkManagement / Item ─────────────────────────────────────────────
    public const string WorkManagement_Item_FieldValueMustBeNumber = "WorkManagement_Item_FieldValueMustBeNumber";

    // ── Automation / ActionValidator ──────────────────────────────────────
    public const string Automation_Action_InvalidConfigJson = "Automation_Action_InvalidConfigJson";
    public const string Automation_Action_ConfigCannotBeNullJson = "Automation_Action_ConfigCannotBeNullJson";
    public const string Automation_Action_InvalidType = "Automation_Action_InvalidType";
    public const string Automation_ActionValidator_UnknownActionType = "Automation_ActionValidator_UnknownActionType";
    public const string Automation_ActionValidator_InvalidWebhookJson = "Automation_ActionValidator_InvalidWebhookJson";
    public const string Automation_ActionValidator_InvalidSendEmailJson = "Automation_ActionValidator_InvalidSendEmailJson";
    public const string Automation_ActionValidator_InvalidSlackMessageJson = "Automation_ActionValidator_InvalidSlackMessageJson";
    public const string Automation_ActionValidator_InvalidUpdateFieldJson = "Automation_ActionValidator_InvalidUpdateFieldJson";
    public const string Automation_ActionValidator_InvalidCreateItemJson = "Automation_ActionValidator_InvalidCreateItemJson";
    public const string Automation_ActionValidator_InvalidMoveItemJson = "Automation_ActionValidator_InvalidMoveItemJson";
    public const string Automation_ActionValidator_InvalidNotifyMemberJson = "Automation_ActionValidator_InvalidNotifyMemberJson";

    // ── Automation / TriggerValidator ─────────────────────────────────────
    public const string Automation_Trigger_InvalidConfigJson = "Automation_Trigger_InvalidConfigJson";
    public const string Automation_Trigger_ConfigCannotBeNullJson = "Automation_Trigger_ConfigCannotBeNullJson";
    public const string Automation_Trigger_InvalidType = "Automation_Trigger_InvalidType";
    public const string Automation_Condition_ConfigMustBeValidJson = "Automation_Condition_ConfigMustBeValidJson";
    public const string Automation_Condition_InvalidConfigJson = "Automation_Condition_InvalidConfigJson";
    public const string Automation_TriggerValidator_UnknownTriggerType = "Automation_TriggerValidator_UnknownTriggerType";
    public const string Automation_TriggerValidator_InvalidFieldChangedJson = "Automation_TriggerValidator_InvalidFieldChangedJson";
    public const string Automation_TriggerValidator_InvalidItemMovedToGroupJson = "Automation_TriggerValidator_InvalidItemMovedToGroupJson";
    public const string Automation_TriggerValidator_InvalidScheduleTriggerJson = "Automation_TriggerValidator_InvalidScheduleTriggerJson";

    // ── Automation / Step ─────────────────────────────────────────────────
    public const string Automation_Step_CannotStartUnlessQueued = "Automation_Step_CannotStartUnlessQueued";
    public const string Automation_Step_CannotSucceedUnlessRunning = "Automation_Step_CannotSucceedUnlessRunning";
    public const string Automation_Step_CannotFailUnlessRunning = "Automation_Step_CannotFailUnlessRunning";

    // ── Automation / AgentRun ─────────────────────────────────────────────
    public const string Automation_AgentRun_CannotStartUnlessQueued = "Automation_AgentRun_CannotStartUnlessQueued";
    public const string Automation_AgentRun_CannotSucceedUnlessRunning = "Automation_AgentRun_CannotSucceedUnlessRunning";
    public const string Automation_AgentRun_CannotFailUnlessRunning = "Automation_AgentRun_CannotFailUnlessRunning";
    public const string Automation_AgentRun_CannotCancelUnlessQueuedOrRunning = "Automation_AgentRun_CannotCancelUnlessQueuedOrRunning";

    // ── Automation / Agent ────────────────────────────────────────────────
    public const string Automation_Agent_InvalidInstructionJson = "Automation_Agent_InvalidInstructionJson";
    public const string Automation_Agent_InvalidModelPolicyJson = "Automation_Agent_InvalidModelPolicyJson";
    public const string Automation_Agent_InvalidToolPermissionsJson = "Automation_Agent_InvalidToolPermissionsJson";

    // ── Automation / Template ─────────────────────────────────────────────
    public const string Automation_Template_AlreadyArchived = "Automation_Template_AlreadyArchived";

    // ── Integrations / WebhookDelivery ────────────────────────────────────
    public const string Integrations_WebhookDelivery_CannotMarkSentFromStatus = "Integrations_WebhookDelivery_CannotMarkSentFromStatus";
    public const string Integrations_WebhookDelivery_CannotMarkFailedFromStatus = "Integrations_WebhookDelivery_CannotMarkFailedFromStatus";
    public const string Integrations_WebhookDelivery_MaxRetriesReached = "Integrations_WebhookDelivery_MaxRetriesReached";
}
