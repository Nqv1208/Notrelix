/**
 * The automation rules of a workspace, as the backend defines them: one
 * trigger, one action and a status, not a board's editor. A rule belongs to the
 * whole workspace; "move an item" acts on the one item it names.
 */

/** The triggers the runtime evaluates and a rule may be created with. */
export type WorkspaceAutomationTrigger =
  "ItemCreated" | "ItemAssigned" | "ItemMovedToGroup";

export interface WorkspaceAutomationRuleDto {
  readonly id: string;
  readonly name: string;
  /** The backend's trigger type ("ItemCreated", "ItemAssigned", ...). */
  readonly trigger: string;
  /** The backend's action type ("MoveItem", "Webhook", ...). */
  readonly action: string;
  /** What a "MoveItem" rule moves and where; null for other actions. */
  readonly itemId: string | null;
  readonly targetGroupId: string | null;
  /** The board the rule belongs to; null for a rule made before rules were board-scoped. */
  readonly boardId: string | null;
  /** The trigger's group filter ("an item is moved to this group"). */
  readonly triggerGroupId: string | null;
  readonly enabled: boolean;
  /** Why the platform turned the rule off, when it did. */
  readonly disabledReason: string | null;
  readonly createdAt: string;
}

export type WorkspaceAutomationRunStatus =
  "queued" | "running" | "succeeded" | "failed" | "cancelled";

export interface WorkspaceAutomationRunDto {
  readonly id: string;
  readonly status: WorkspaceAutomationRunStatus;
  readonly attempts: number;
  readonly error: string | null;
  readonly createdAt: string;
}

export interface CreateWorkspaceAutomationInput {
  readonly name: string;
  readonly trigger: WorkspaceAutomationTrigger;
  readonly boardId: string;
  /** For "an item is moved to a group": the group it moves into. */
  readonly triggerGroupId?: string;
  /** The rule moves the item that triggered it to this group of the board. */
  readonly targetGroupId: string;
}

export type WorkspaceAutomationWriteResult =
  | { ok: true }
  | { ok: false; reason: "limit" | "invalid" | "forbidden" | "failed" };
