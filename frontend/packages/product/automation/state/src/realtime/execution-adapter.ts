import type {
  RealtimeEnvelope,
  ModuleRealtimeAdapter,
  ModuleRealtimeContext,
} from "@notrelix/realtime";
import { automationQueryKeys } from "../query/keys";

export type AutomationExecutionEventType =
  | "automation.execution.started"
  | "automation.execution.step-updated"
  | "automation.execution.completed"
  | "automation.execution.failed";

const automationExecutionEventTypes: readonly AutomationExecutionEventType[] = [
  "automation.execution.started",
  "automation.execution.step-updated",
  "automation.execution.completed",
  "automation.execution.failed",
];

export function isAutomationExecutionEventType(
  eventType: string,
): eventType is AutomationExecutionEventType {
  return automationExecutionEventTypes.includes(
    eventType as AutomationExecutionEventType,
  );
}

/**
 * Realtime frames carry no payload, so an execution event is only a change
 * notification: revalidate the Workspace's automation queries over REST
 * instead of patching execution state from the event.
 */
export function createAutomationExecutionRealtimeAdapter(): ModuleRealtimeAdapter {
  return {
    id: "automation-execution-adapter",
    supports(envelope: RealtimeEnvelope): boolean {
      return isAutomationExecutionEventType(envelope.eventType);
    },
    async validateAndHandle(
      envelope: RealtimeEnvelope,
      context: ModuleRealtimeContext,
    ): Promise<void> {
      if (envelope.workspaceId !== context.workspaceId) {
        return;
      }

      await context.invalidateQueries([
        [...automationQueryKeys.all(context.workspaceId)],
      ]);
    },
  };
}
