namespace Notrelix.Domain.WorkManagement;

/// <summary>
/// Rule codes for the WorkManagement bounded context.
/// </summary>
public static class WorkManagementRuleCodes
{
    // ── Board ─────────────────────────────────────────────────────────────
    public const string WorkManagement_Board_CannotRenameArchived = "WorkManagement_Board_CannotRenameArchived";
    public const string WorkManagement_Board_CannotUpdateDescriptionArchived = "WorkManagement_Board_CannotUpdateDescriptionArchived";
    public const string WorkManagement_Board_CannotUpdateBackgroundArchived = "WorkManagement_Board_CannotUpdateBackgroundArchived";
    public const string WorkManagement_Board_CannotChangeVisibilityArchived = "WorkManagement_Board_CannotChangeVisibilityArchived";
    public const string WorkManagement_Board_CannotGenerateIdentityArchived = "WorkManagement_Board_CannotGenerateIdentityArchived";
    public const string WorkManagement_Board_CannotDeleteDefaultGroup = "WorkManagement_Board_CannotDeleteDefaultGroup";

    // ── Field ─────────────────────────────────────────────────────────────
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
    public const string WorkManagement_Field_BelongsToDifferentWorkspace = "WorkManagement_Field_BelongsToDifferentWorkspace";

    // ── FieldSettings ─────────────────────────────────────────────────────
    public const string WorkManagement_FieldSettings_InvalidJsonFormat = "WorkManagement_FieldSettings_InvalidJsonFormat";
    public const string WorkManagement_FieldSettings_NumberMinMustBeNumber = "WorkManagement_FieldSettings_NumberMinMustBeNumber";
    public const string WorkManagement_FieldSettings_NumberMaxMustBeNumber = "WorkManagement_FieldSettings_NumberMaxMustBeNumber";
    public const string WorkManagement_FieldSettings_TextMaxLengthMustBeNumber = "WorkManagement_FieldSettings_TextMaxLengthMustBeNumber";
    public const string WorkManagement_FieldSettings_DateIncludeTimeMustBeBoolean = "WorkManagement_FieldSettings_DateIncludeTimeMustBeBoolean";
    public const string WorkManagement_FieldSettings_StatusMustIncludeTransitions = "WorkManagement_FieldSettings_StatusMustIncludeTransitions";

    // ── FieldValue ────────────────────────────────────────────────────────
    public const string WorkManagement_FieldValue_CannotBeNull = "WorkManagement_FieldValue_CannotBeNull";
    public const string WorkManagement_FieldValue_InvalidJsonFormat = "WorkManagement_FieldValue_InvalidJsonFormat";
    public const string WorkManagement_FieldValue_InvalidStringValue = "WorkManagement_FieldValue_InvalidStringValue";
    public const string WorkManagement_FieldValue_InvalidStringFormat = "WorkManagement_FieldValue_InvalidStringFormat";
    public const string WorkManagement_FieldValue_InvalidLinkValue = "WorkManagement_FieldValue_InvalidLinkValue";
    public const string WorkManagement_FieldValue_TextExceedsMaxLength = "WorkManagement_FieldValue_TextExceedsMaxLength";
    public const string WorkManagement_FieldValue_NumberBelowMin = "WorkManagement_FieldValue_NumberBelowMin";
    public const string WorkManagement_FieldValue_NumberAboveMax = "WorkManagement_FieldValue_NumberAboveMax";
    public const string WorkManagement_FieldValue_InvalidBooleanValue = "WorkManagement_FieldValue_InvalidBooleanValue";
    public const string WorkManagement_FieldValue_InvalidSelectValue = "WorkManagement_FieldValue_InvalidSelectValue";
    public const string WorkManagement_FieldValue_InvalidMultiSelectValue = "WorkManagement_FieldValue_InvalidMultiSelectValue";
    public const string WorkManagement_FieldValue_InvalidDateValue = "WorkManagement_FieldValue_InvalidDateValue";
    public const string WorkManagement_FieldValue_CalculatedFieldCannotBeWritten = "WorkManagement_FieldValue_CalculatedFieldCannotBeWritten";
    public const string WorkManagement_FieldValue_UnknownFieldType = "WorkManagement_FieldValue_UnknownFieldType";

    // ── Item ──────────────────────────────────────────────────────────────
    public const string WorkManagement_Item_ParentMustBelongToSameBoard = "WorkManagement_Item_ParentMustBelongToSameBoard";
    public const string WorkManagement_Item_DueDateMustBeAfterStartDate = "WorkManagement_Item_DueDateMustBeAfterStartDate";
    public const string WorkManagement_Item_CannotBeOwnParent = "WorkManagement_Item_CannotBeOwnParent";
    public const string WorkManagement_Item_ParentAssignmentWouldCreateCycle = "WorkManagement_Item_ParentAssignmentWouldCreateCycle";
    public const string WorkManagement_Item_CannotModifyArchived = "WorkManagement_Item_CannotModifyArchived";
    public const string WorkManagement_Item_FieldValueMustBeNumber = "WorkManagement_Item_FieldValueMustBeNumber";

    // ── Dependency ────────────────────────────────────────────────────────
    public const string WorkManagement_Dependency_CannotDependOnSelf = "WorkManagement_Dependency_CannotDependOnSelf";
    public const string WorkManagement_Dependency_CannotCreateCycle = "WorkManagement_Dependency_CannotCreateCycle";

    // ── View ──────────────────────────────────────────────────────────────
    public const string WorkManagement_View_KanbanMustUseKanbanConfig = "WorkManagement_View_KanbanMustUseKanbanConfig";
    public const string WorkManagement_View_TableMustUseTableConfig = "WorkManagement_View_TableMustUseTableConfig";
    public const string WorkManagement_View_CalendarMustUseCalendarConfig = "WorkManagement_View_CalendarMustUseCalendarConfig";
    public const string WorkManagement_View_TimelineMustUseTimelineConfig = "WorkManagement_View_TimelineMustUseTimelineConfig";
    public const string WorkManagement_View_CannotDeleteDefault = "WorkManagement_View_CannotDeleteDefault";
    public const string WorkManagement_View_DuplicateFilterRules = "WorkManagement_View_DuplicateFilterRules";
    public const string WorkManagement_View_DuplicateSortRules = "WorkManagement_View_DuplicateSortRules";

    // ── Kanban ────────────────────────────────────────────────────────────
    public const string WorkManagement_Kanban_InvalidColumnField = "WorkManagement_Kanban_InvalidColumnField";
    public const string WorkManagement_Kanban_MustHaveVisibleField = "WorkManagement_Kanban_MustHaveVisibleField";
    public const string WorkManagement_Kanban_VisibleFieldIdsCannotBeEmpty = "WorkManagement_Kanban_VisibleFieldIdsCannotBeEmpty";
    public const string WorkManagement_Kanban_SwimlaneFieldIdCannotBeEmpty = "WorkManagement_Kanban_SwimlaneFieldIdCannotBeEmpty";

    // ── Form ──────────────────────────────────────────────────────────────
    public const string WorkManagement_Form_CannotPublishClosed = "WorkManagement_Form_CannotPublishClosed";
    public const string WorkManagement_Form_CannotPublishNoQuestions = "WorkManagement_Form_CannotPublishNoQuestions";
    public const string WorkManagement_Form_CannotSubmitToDraft = "WorkManagement_Form_CannotSubmitToDraft";
    public const string WorkManagement_Form_CannotSubmitToClosed = "WorkManagement_Form_CannotSubmitToClosed";
    public const string WorkManagement_Form_CannotAddQuestionToClosed = "WorkManagement_Form_CannotAddQuestionToClosed";
    public const string WorkManagement_Form_DuplicateQuestionKey = "WorkManagement_Form_DuplicateQuestionKey";

    // ── FormQuestion ──────────────────────────────────────────────────────
    public const string WorkManagement_FormQuestion_InvalidConfigJson = "WorkManagement_FormQuestion_InvalidConfigJson";
    public const string WorkManagement_FormQuestion_MaxLengthInvalidForType = "WorkManagement_FormQuestion_MaxLengthInvalidForType";
    public const string WorkManagement_FormQuestion_MinMaxInvalidForType = "WorkManagement_FormQuestion_MinMaxInvalidForType";
    public const string WorkManagement_FormQuestion_MaxFileSizeInvalidForType = "WorkManagement_FormQuestion_MaxFileSizeInvalidForType";
    public const string WorkManagement_FormQuestion_MaxLengthMustBePositive = "WorkManagement_FormQuestion_MaxLengthMustBePositive";
    public const string WorkManagement_FormQuestion_MaxFileSizeMustBePositive = "WorkManagement_FormQuestion_MaxFileSizeMustBePositive";
    public const string WorkManagement_FormQuestion_MinCannotExceedMax = "WorkManagement_FormQuestion_MinCannotExceedMax";

    // ── FormSubmission ────────────────────────────────────────────────────
    public const string WorkManagement_FormSubmission_CannotRejectUnlessAccepted = "WorkManagement_FormSubmission_CannotRejectUnlessAccepted";
    public const string WorkManagement_FormSubmission_CannotMarkSpamUnlessAccepted = "WorkManagement_FormSubmission_CannotMarkSpamUnlessAccepted";
    public const string WorkManagement_FormSubmission_CannotProcessUnlessAccepted = "WorkManagement_FormSubmission_CannotProcessUnlessAccepted";
    public const string WorkManagement_FormSubmission_AlreadyDeleted = "WorkManagement_FormSubmission_AlreadyDeleted";

    // ── Approval ──────────────────────────────────────────────────────────
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
    public const string WorkManagement_Approval_StepNotAssignedToYou = "WorkManagement_Approval_StepNotAssignedToYou";

    // ── Template ──────────────────────────────────────────────────────────
    public const string WorkManagement_BoardTemplate_CannotDraftArchived = "WorkManagement_BoardTemplate_CannotDraftArchived";
    public const string WorkManagement_BoardTemplate_CannotPublishArchived = "WorkManagement_BoardTemplate_CannotPublishArchived";
    public const string WorkManagement_BoardTemplate_CanOnlyRestoreArchived = "WorkManagement_BoardTemplate_CanOnlyRestoreArchived";

    // ── TimeTracking ──────────────────────────────────────────────────────
    public const string WorkManagement_TimeTracking_CannotStopNotRunning = "WorkManagement_TimeTracking_CannotStopNotRunning";
    public const string WorkManagement_TimeTracking_EndTimeMustBeAfterStart = "WorkManagement_TimeTracking_EndTimeMustBeAfterStart";

    // ── Checklist ─────────────────────────────────────────────────────────
    public const string WorkManagement_Checklist_ItemNotFound = "WorkManagement_Checklist_ItemNotFound";

    // ── Relation ──────────────────────────────────────────────────────────
    public const string WorkManagement_Relation_SourceAndTargetMustBeDifferent = "WorkManagement_Relation_SourceAndTargetMustBeDifferent";
    public const string WorkManagement_Relation_CannotCreateSelfReferencing = "WorkManagement_Relation_CannotCreateSelfReferencing";
    public const string WorkManagement_Relation_CannotResumeBroken = "WorkManagement_Relation_CannotResumeBroken";

    // ── Connection ────────────────────────────────────────────────────────
    public const string WorkManagement_Connection_CannotConnectToSelf = "WorkManagement_Connection_CannotConnectToSelf";
}
