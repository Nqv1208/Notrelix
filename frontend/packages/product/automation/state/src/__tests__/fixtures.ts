import type { AutomationRule } from "@notrelix/automation-core";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import type { AutomationRepositories } from "../data/repositories";
import type {
  AutomationExecutionEventPayload,
  AutomationExecutionEventType,
} from "../realtime/execution-adapter";

// Test-local fixtures. The product testing package depends on this package,
// so state tests must not import it back (workspace dependency cycle).

export function buildAutomationRule(
  overrides: Partial<AutomationRule> = {},
): AutomationRule {
  return {
    id: "rule-1",
    workspaceId: "workspace-1",
    boardId: "board-1",
    name: "Archive completed work",
    description: "Archive cards when their status changes to Done.",
    triggerType: "card_status_changed",
    triggerConfig: { to: "done" },
    actionType: "archive_card",
    actionConfig: {},
    isEnabled: true,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}

export function createFakeAutomationRepositories(
  input: {
    rules?: AutomationRule[];
  } = {},
): AutomationRepositories {
  const rules = new Map<string, AutomationRule>(
    (input.rules ?? [buildAutomationRule()]).map((rule) => [rule.id, rule]),
  );

  const notUsed = (method: string) => (): never => {
    throw new Error(`Unexpected repository call in this test: ${method}`);
  };

  return {
    rules: {
      listByWorkspace: notUsed("rules.listByWorkspace"),
      async getDetail(ruleId: string) {
        const rule = rules.get(ruleId);
        if (!rule) throw new Error(`Automation rule not found: ${ruleId}`);
        return rule;
      },
      create: notUsed("rules.create"),
      update: notUsed("rules.update"),
      async enable(ruleId: string) {
        const rule = rules.get(ruleId);
        if (!rule) throw new Error(`Automation rule not found: ${ruleId}`);
        const next = { ...rule, isEnabled: true };
        rules.set(ruleId, next);
        return next;
      },
      async disable(ruleId: string) {
        const rule = rules.get(ruleId);
        if (!rule) throw new Error(`Automation rule not found: ${ruleId}`);
        const next = { ...rule, isEnabled: false };
        rules.set(ruleId, next);
        return next;
      },
      delete: notUsed("rules.delete"),
      test: notUsed("rules.test"),
    },
    executions: {
      listHistory: notUsed("executions.listHistory"),
      getDetail: notUsed("executions.getDetail"),
    },
    templates: {
      listTemplates: notUsed("templates.listTemplates"),
    },
  };
}

export function buildAutomationExecutionEvent(
  overrides: {
    eventType?: AutomationExecutionEventType;
    workspaceId?: string;
    sequence?: number;
    payload?: Partial<AutomationExecutionEventPayload>;
  } = {},
): RealtimeEnvelope<AutomationExecutionEventPayload> {
  const eventType = overrides.eventType ?? "automation.execution.started";
  const sequence = overrides.sequence ?? 1;
  return {
    schemaVersion: 1,
    eventId: `event-${sequence}`,
    eventType,
    workspaceId: overrides.workspaceId ?? "workspace-1",
    correlationId: `correlation-${sequence}`,
    timestamp: "2026-01-01T00:00:00.000Z",
    sequence,
    payload: {
      executionId: "execution-1",
      ruleId: "rule-1",
      status:
        eventType === "automation.execution.failed" ? "failed" : "running",
      sequence,
      ...overrides.payload,
    },
  };
}
