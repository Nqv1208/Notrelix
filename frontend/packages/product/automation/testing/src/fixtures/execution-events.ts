import type { RealtimeEnvelope } from "@notrelix/realtime";
import type { AutomationExecutionEventType } from "@notrelix/automation-state";

export function buildAutomationExecutionEvent(
  overrides: {
    eventType?: AutomationExecutionEventType;
    workspaceId?: string;
    streamVersion?: number;
  } = {},
): RealtimeEnvelope {
  const eventType = overrides.eventType ?? "automation.execution.started";
  const streamVersion = overrides.streamVersion ?? 1;
  const workspaceId = overrides.workspaceId ?? "workspace-1";
  return {
    schemaVersion: 1,
    eventId: `event-${streamVersion}`,
    eventType,
    changeKind: "updated",
    subscriptionId: `workspace:${workspaceId}`,
    streamKey: `workspace:Workspace:${workspaceId}`,
    streamVersion,
    resourceKind: "AutomationExecution",
    resourceId: "execution-1",
    aggregateId: "execution-1",
    workspaceId,
    correlationId: `correlation-${streamVersion}`,
    timestamp: "2026-01-01T00:00:00.000Z",
  };
}
