export type AutomationTriggerType =
  | "card_status_changed"
  | "card_priority_changed"
  | "card_created"
  | "card_deleted"
  | "field_updated";

export type AutomationActionType =
  | "archive_card"
  | "remove_members"
  | "notify_owner"
  | "send_email"
  | "move_to_group"
  | "set_field_value";

export interface AutomationRule {
  id: string;
  workspaceId: string;
  boardId: string;
  name: string;
  description: string;
  triggerType: AutomationTriggerType;
  triggerConfig: Record<string, unknown>;
  actionType: AutomationActionType;
  actionConfig: Record<string, unknown>;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AutomationRun {
  id: string;
  ruleId: string;
  status: "queued" | "running" | "succeeded" | "failed" | "cancelled";
  triggeredBy: string;
  startedAt: string;
  completedAt?: string;
  error?: string;
}

export type AutomationExecutionStatus = AutomationRun["status"];

export interface AutomationExecutionStep {
  id: string;
  executionId: string;
  name: string;
  status: AutomationExecutionStatus;
  startedAt?: string;
  completedAt?: string;
  error?: string;
}

export interface AutomationExecution extends AutomationRun {
  workspaceId: string;
  boardId?: string;
  steps: AutomationExecutionStep[];
  sequence?: number;
  version?: number;
}

export interface AutomationTemplate {
  id: string;
  name: string;
  description: string;
  triggerType: AutomationTriggerType;
  actionType: AutomationActionType;
  triggerConfig: Record<string, unknown>;
  actionConfig: Record<string, unknown>;
}

export interface AutomationCursorPage<TItem> {
  items: TItem[];
  nextCursor?: string;
}

export interface CreateAutomationRuleInput {
  workspaceId: string;
  boardId: string;
  name: string;
  description?: string;
  triggerType: AutomationTriggerType;
  triggerConfig: Record<string, unknown>;
  actionType: AutomationActionType;
  actionConfig: Record<string, unknown>;
}

export interface UpdateAutomationRuleInput {
  ruleId: string;
  name?: string;
  description?: string;
  triggerConfig?: Record<string, unknown>;
  actionConfig?: Record<string, unknown>;
}
