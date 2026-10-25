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
}

export class MockWorkspaceAutomations {
  private rulesByWorkspace = new Map<string, MockAutomationRule[]>();
  private sequence = 0;

  list(workspaceId: string): MockAutomationRule[] {
    return [...(this.rulesByWorkspace.get(workspaceId) ?? [])].sort((a, b) =>
      a.name.localeCompare(b.name),
    );
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
    input: {
      name: string;
      triggerEvent: string;
      actionType: string;
      configuration: string;
    },
    userId: string,
  ): MockAutomationRule {
    this.sequence += 1;
    const action = JSON.stringify({
      Type: input.actionType,
      Configuration: input.configuration,
      SchemaVersion: 1,
    });
    const rule: MockAutomationRule = {
      id: `auto-${String(this.sequence).padStart(5, "0")}`,
      workspaceId,
      name: input.name.trim(),
      triggerType: input.triggerEvent,
      actionType: input.actionType,
      configuration: JSON.stringify({
        Trigger: {
          Type: input.triggerEvent,
          Configuration: input.configuration,
          SchemaVersion: 1,
        },
        Action: JSON.parse(action) as unknown,
        Condition: null,
        SchemaVersion: 1,
      }),
      isEnabled: false,
      createdAt: isoOfMs(wallClockMs()),
      updatedAt: null,
      executorUserId: userId,
      disabledReason: null,
    };
    this.rulesByWorkspace.set(workspaceId, [
      ...(this.rulesByWorkspace.get(workspaceId) ?? []),
      rule,
    ]);
    return rule;
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
