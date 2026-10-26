/**
 * The automation rules of a board, as the backend defines them: one trigger,
 * optional conditions that must all hold, and up to five actions that run in
 * order on the item that triggered the rule. No branching, no loops.
 */

/** The triggers the runtime evaluates and a rule may be created with. */
export type WorkspaceAutomationTrigger =
  "ItemCreated" | "ItemAssigned" | "ItemMovedToGroup";

/** The most actions one rule runs. */
export const MAX_AUTOMATION_ACTIONS = 5;

/** An action of a rule; the ones the runtime can execute. */
export type AutomationActionInput =
  | { readonly type: "MoveItem"; readonly groupId: string }
  | { readonly type: "ChangeStatus"; readonly optionId: string }
  | { readonly type: "AssignPerson"; readonly userId: string }
  | {
      readonly type: "UpdateField";
      readonly fieldId: string;
      readonly value: string;
    }
  | {
      readonly type: "NotifyMember";
      readonly userId: string;
      readonly message: string;
    }
  | {
      readonly type: "SendEmail";
      readonly userId: string;
      readonly subject: string;
      readonly body: string;
    };

export type AutomationActionType = AutomationActionInput["type"];

/** An "equals" test on the item that triggered the rule. */
export type AutomationConditionInput =
  | { readonly type: "group"; readonly groupId: string }
  | { readonly type: "status"; readonly optionId: string }
  | {
      readonly type: "field";
      readonly fieldId: string;
      readonly value: string;
    };

export interface WorkspaceAutomationRuleDto {
  readonly id: string;
  readonly name: string;
  /** The backend's trigger type ("ItemCreated", "ItemAssigned", ...). */
  readonly trigger: string;
  /** The actions in the order they run (those the editor knows). */
  readonly actions: readonly AutomationActionInput[];
  /** The conditions; all must hold. */
  readonly conditions: readonly AutomationConditionInput[];
  /** The board the rule belongs to; null for a rule made before rules were board-scoped. */
  readonly boardId: string | null;
  /** The trigger's group filter ("an item is moved to this group"). */
  readonly triggerGroupId: string | null;
  readonly enabled: boolean;
  /** Why the platform turned the rule off, when it did. */
  readonly disabledReason: string | null;
  /** The rule's version: an edit creates a new one. */
  readonly version: number;
  readonly createdAt: string;
}

export type WorkspaceAutomationRunStatus =
  "queued" | "running" | "succeeded" | "failed" | "cancelled";

/** What one action of a run did. */
export interface WorkspaceAutomationRunStepDto {
  readonly index: number;
  readonly type: string;
  readonly status: "succeeded" | "failed";
  readonly error: string | null;
}

export interface WorkspaceAutomationRunDto {
  readonly id: string;
  readonly status: WorkspaceAutomationRunStatus;
  readonly attempts: number;
  readonly error: string | null;
  /** The rule version the run started on. */
  readonly ruleVersion: number | null;
  readonly steps: readonly WorkspaceAutomationRunStepDto[];
  readonly createdAt: string;
}

export interface WorkspaceAutomationDefinition {
  readonly name: string;
  readonly trigger: WorkspaceAutomationTrigger;
  /** For "an item is moved to a group": the group it moves into. */
  readonly triggerGroupId?: string;
  readonly conditions: readonly AutomationConditionInput[];
  readonly actions: readonly AutomationActionInput[];
}

export interface CreateWorkspaceAutomationInput extends WorkspaceAutomationDefinition {
  readonly boardId: string;
}

export type WorkspaceAutomationWriteResult =
  | { ok: true }
  | { ok: false; reason: "limit" | "invalid" | "forbidden" | "failed" };
