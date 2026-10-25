/**
 * The automation rules of a workspace in the mock world, shaped like the
 * producer's `AutomationRuleDto` and `AutomationExecutionDto`: one trigger, one
 * action, no board. A rule starts off; the mock never runs one, so its run
 * history stays empty.
 */

import { isoOfMs, wallClockMs } from "./clock";

export interface MockAutomationRule {
  id: string;
  workspaceId: string;
  boardId: string | null;
  name: string;
  triggerType: string;
  actionType: string;
  /** The producer's saved configuration, whose action carries its own JSON text. */
  configuration: string;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
  executorUserId: string | null;
  disabledReason: string | null;
  /** The trigger's own JSON text (the group filter). */
  triggerConfiguration: string | null;
  /** The rule's version: an edit raises it. */
  version: number;
  /** The actions in the order they run, each with its own JSON text. */
  actions: { type: string; configuration: string | null }[];
  /** The conditions' JSON text, `{"all":[…]}`. */
  conditions: string | null;
  /** A deleted rule is gone from the lists. */
  removed: boolean;
}

export interface MockAutomationDefinition {
  name: string;
  triggerEvent: string;
  triggerConfiguration: string | null;
  actions: { type: string; configuration: string | null }[];
  conditions: string | null;
}

function savedConfiguration(
  input: MockAutomationDefinition,
  boardScoped: boolean,
): string {
  const [first] = input.actions;
  return JSON.stringify({
    Trigger: {
      Type: input.triggerEvent,
      Configuration: boardScoped
        ? input.triggerConfiguration
        : (first?.configuration ?? null),
      SchemaVersion: 1,
    },
    Action: {
      Type: first?.type ?? "",
      Configuration: first?.configuration ?? null,
      SchemaVersion: 1,
    },
    MoreActions: input.actions.slice(1).map((action) => ({
      Type: action.type,
      Configuration: action.configuration,
      SchemaVersion: 1,
    })),
    Condition: input.conditions
      ? { RawJson: input.conditions, SchemaVersion: 1 }
      : null,
    SchemaVersion: 1,
  });
}

export class MockWorkspaceAutomations {
  private rulesByWorkspace = new Map<string, MockAutomationRule[]>();
  private sequence = 0;

  list(workspaceId: string, boardId?: string | null): MockAutomationRule[] {
    return [...(this.rulesByWorkspace.get(workspaceId) ?? [])]
      .filter((rule) => !rule.removed && (!boardId || rule.boardId === boardId))
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  find(ruleId: string): MockAutomationRule | undefined {
    for (const rules of this.rulesByWorkspace.values()) {
      const rule = rules.find((candidate) => candidate.id === ruleId);
      if (rule) return rule;
    }
    return undefined;
  }

  create(
    workspaceId: string,
    input: MockAutomationDefinition & { boardId?: string | null },
    userId: string,
  ): MockAutomationRule {
    this.sequence += 1;
    const [first] = input.actions;
    const rule: MockAutomationRule = {
      id: `auto-${String(this.sequence).padStart(5, "0")}`,
      workspaceId,
      boardId: input.boardId ?? null,
      name: input.name.trim(),
      triggerType: input.triggerEvent,
      actionType: first?.type ?? "",
      configuration: savedConfiguration(input, Boolean(input.boardId)),
      isEnabled: false,
      createdAt: isoOfMs(wallClockMs()),
      updatedAt: null,
      executorUserId: userId,
      disabledReason: null,
      triggerConfiguration: input.triggerConfiguration,
      version: 1,
      actions: input.actions,
      conditions: input.conditions,
      removed: false,
    };
    this.rulesByWorkspace.set(workspaceId, [
      ...(this.rulesByWorkspace.get(workspaceId) ?? []),
      rule,
    ]);
    return rule;
  }

  /** An edit is a new version; the editor becomes the executor. */
  update(
    rule: MockAutomationRule,
    input: MockAutomationDefinition,
    userId: string,
  ): void {
    const [first] = input.actions;
    rule.name = input.name.trim();
    rule.triggerType = input.triggerEvent;
    rule.actionType = first?.type ?? "";
    rule.configuration = savedConfiguration(input, Boolean(rule.boardId));
    rule.triggerConfiguration = input.triggerConfiguration;
    rule.actions = input.actions;
    rule.conditions = input.conditions;
    rule.executorUserId = userId;
    rule.version += 1;
    rule.updatedAt = isoOfMs(wallClockMs());
  }

  remove(rule: MockAutomationRule): void {
    rule.removed = true;
    rule.isEnabled = false;
    rule.updatedAt = isoOfMs(wallClockMs());
  }

  setEnabled(rule: MockAutomationRule, enabled: boolean): void {
    rule.isEnabled = enabled;
    rule.disabledReason = null;
    rule.updatedAt = isoOfMs(wallClockMs());
  }

  reset(): void {
    this.rulesByWorkspace.clear();
    this.sequence = 0;
  }
}
