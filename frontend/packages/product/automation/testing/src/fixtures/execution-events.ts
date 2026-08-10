import type { RealtimeEnvelope } from "@notrelix/realtime";
import type {
  AutomationExecutionEventPayload,
  AutomationExecutionEventType,
} from "@notrelix/automation-state";

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
