namespace Notrelix.Domain.Automation;

/// <summary>
/// Rule codes for the Automation bounded context.
/// </summary>
public static class AutomationRuleCodes
{
    // ── Rule ──────────────────────────────────────────────────────────────
    public const string Automation_Rule_NameCannotBeEmpty = "Automation_Rule_NameCannotBeEmpty";
    public const string Automation_Rule_MustHaveConfiguration = "Automation_Rule_MustHaveConfiguration";
    public const string Automation_Rule_MustHaveTrigger = "Automation_Rule_MustHaveTrigger";
    public const string Automation_Rule_MustHaveAction = "Automation_Rule_MustHaveAction";

    // ── Execution ─────────────────────────────────────────────────────────
    public const string Automation_Execution_CannotStartUnlessQueued = "Automation_Execution_CannotStartUnlessQueued";
    public const string Automation_Execution_CannotSucceedUnlessRunning = "Automation_Execution_CannotSucceedUnlessRunning";
    public const string Automation_Execution_CannotFailUnlessRunning = "Automation_Execution_CannotFailUnlessRunning";
    public const string Automation_Execution_ErrorRequiredOnFail = "Automation_Execution_ErrorRequiredOnFail";
    public const string Automation_Execution_CannotCancelUnlessQueuedOrRunning = "Automation_Execution_CannotCancelUnlessQueuedOrRunning";

    // ── Agent ─────────────────────────────────────────────────────────────
    public const string Automation_Agent_InvalidStatusTransition = "Automation_Agent_InvalidStatusTransition";
    public const string Automation_Agent_InvalidInstructionJson = "Automation_Agent_InvalidInstructionJson";
    public const string Automation_Agent_InvalidModelPolicyJson = "Automation_Agent_InvalidModelPolicyJson";
    public const string Automation_Agent_InvalidToolPermissionsJson = "Automation_Agent_InvalidToolPermissionsJson";

    // ── ScheduledJob ──────────────────────────────────────────────────────
    public const string Automation_ScheduledJob_CannotCompleteFromStatus = "Automation_ScheduledJob_CannotCompleteFromStatus";
    public const string Automation_ScheduledJob_CannotFailFromStatus = "Automation_ScheduledJob_CannotFailFromStatus";

    // ── ActionValidator ───────────────────────────────────────────────────
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

    // ── TriggerValidator ──────────────────────────────────────────────────
    public const string Automation_Trigger_InvalidConfigJson = "Automation_Trigger_InvalidConfigJson";
    public const string Automation_Trigger_ConfigCannotBeNullJson = "Automation_Trigger_ConfigCannotBeNullJson";
    public const string Automation_Trigger_InvalidType = "Automation_Trigger_InvalidType";
    public const string Automation_Condition_ConfigMustBeValidJson = "Automation_Condition_ConfigMustBeValidJson";
    public const string Automation_Condition_InvalidConfigJson = "Automation_Condition_InvalidConfigJson";
    public const string Automation_TriggerValidator_UnknownTriggerType = "Automation_TriggerValidator_UnknownTriggerType";
    public const string Automation_TriggerValidator_InvalidFieldChangedJson = "Automation_TriggerValidator_InvalidFieldChangedJson";
    public const string Automation_TriggerValidator_InvalidItemMovedToGroupJson = "Automation_TriggerValidator_InvalidItemMovedToGroupJson";
    public const string Automation_TriggerValidator_InvalidScheduleTriggerJson = "Automation_TriggerValidator_InvalidScheduleTriggerJson";

    // ── Step ──────────────────────────────────────────────────────────────
    public const string Automation_Step_CannotStartUnlessQueued = "Automation_Step_CannotStartUnlessQueued";
    public const string Automation_Step_CannotSucceedUnlessRunning = "Automation_Step_CannotSucceedUnlessRunning";
    public const string Automation_Step_CannotFailUnlessRunning = "Automation_Step_CannotFailUnlessRunning";

    // ── AgentRun ──────────────────────────────────────────────────────────
    public const string Automation_AgentRun_CannotStartUnlessQueued = "Automation_AgentRun_CannotStartUnlessQueued";
    public const string Automation_AgentRun_CannotSucceedUnlessRunning = "Automation_AgentRun_CannotSucceedUnlessRunning";
    public const string Automation_AgentRun_CannotFailUnlessRunning = "Automation_AgentRun_CannotFailUnlessRunning";
    public const string Automation_AgentRun_CannotCancelUnlessQueuedOrRunning = "Automation_AgentRun_CannotCancelUnlessQueuedOrRunning";

    // ── Template ──────────────────────────────────────────────────────────
    public const string Automation_Template_AlreadyArchived = "Automation_Template_AlreadyArchived";
}
