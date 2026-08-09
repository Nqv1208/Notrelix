import type { QueryClient } from "@tanstack/react-query";
import type {
  RealtimeEnvelope,
  ModuleRealtimeAdapter,
  ModuleRealtimeContext,
} from "@notrelix/realtime";
import type {
  AutomationExecution,
  AutomationExecutionStep,
} from "@notrelix/automation-core";
import { automationQueryKeys } from "../query/keys";

export type AutomationExecutionEventType =
  | "automation.execution.started"
  | "automation.execution.step-updated"
  | "automation.execution.completed"
  | "automation.execution.failed";

export interface AutomationExecutionEventPayload {
  executionId: string;
  ruleId: string;
  status?: AutomationExecution["status"];
  boardId?: string;
  sequence?: number;
  version?: number;
  step?: AutomationExecutionStep;
  error?: string;
}

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

export function createAutomationExecutionRealtimeAdapter(
  queryClient: QueryClient,
): ModuleRealtimeAdapter {
  return {
    id: "automation-execution-adapter",
    supports(envelope: RealtimeEnvelope<unknown>): boolean {
      return isAutomationExecutionEventType(envelope.eventType);
    },
    async validateAndHandle(
      envelope: RealtimeEnvelope<unknown>,
      context: ModuleRealtimeContext,
    ): Promise<void> {
      if (envelope.workspaceId !== context.workspaceId) {
        return;
      }

      const payload = parseAutomationExecutionPayload(envelope.payload);
      if (!payload) {
        await context.invalidateQueries([
          [...automationQueryKeys.all(context.workspaceId)],
        ]);
        return;
      }

      const detailKey = automationQueryKeys.executionDetail(
        context.workspaceId,
        payload.executionId,
      );
      const existing = queryClient.getQueryData<AutomationExecution>(detailKey);
      if (existing && isOlderOrEqual(payload, existing)) {
        return;
      }

      queryClient.setQueryData<AutomationExecution | undefined>(
        detailKey,
        (current) => reconcileExecutionEvent(current, envelope, payload),
      );

      await context.invalidateQueries([
        [
          ...automationQueryKeys.executionHistory(
            context.workspaceId,
            payload.ruleId,
          ),
        ],
      ]);
    },
  };
}

function parseAutomationExecutionPayload(
  payload: unknown,
): AutomationExecutionEventPayload | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const candidate = payload as Partial<AutomationExecutionEventPayload>;
  if (
    typeof candidate.executionId !== "string" ||
    typeof candidate.ruleId !== "string"
  ) {
    return null;
  }
  return {
    executionId: candidate.executionId,
    ruleId: candidate.ruleId,
    status: candidate.status,
    boardId: candidate.boardId,
    sequence: candidate.sequence,
    version: candidate.version,
    step: candidate.step,
    error: candidate.error,
  };
}

function isOlderOrEqual(
  payload: AutomationExecutionEventPayload,
  existing: AutomationExecution,
): boolean {
  if (payload.sequence !== undefined && existing.sequence !== undefined) {
    return payload.sequence <= existing.sequence;
  }
  if (payload.version !== undefined && existing.version !== undefined) {
    return payload.version <= existing.version;
  }
  return false;
}

function reconcileExecutionEvent(
  current: AutomationExecution | undefined,
  envelope: RealtimeEnvelope<unknown>,
  payload: AutomationExecutionEventPayload,
): AutomationExecution {
  const status = payload.status ?? statusFromEvent(envelope.eventType);
  const previousSteps = current?.steps ?? [];
  return {
    id: payload.executionId,
    ruleId: payload.ruleId,
    workspaceId: envelope.workspaceId,
    boardId: payload.boardId ?? current?.boardId,
    status,
    triggeredBy: current?.triggeredBy ?? "system",
    startedAt: current?.startedAt ?? envelope.timestamp,
    completedAt: isTerminalStatus(status)
      ? envelope.timestamp
      : current?.completedAt,
    error: payload.error ?? current?.error,
    steps: payload.step
      ? upsertStep(previousSteps, payload.step)
      : previousSteps,
    sequence: payload.sequence ?? envelope.sequence ?? current?.sequence,
    version: payload.version ?? envelope.aggregateVersion ?? current?.version,
  };
}

function statusFromEvent(eventType: string): AutomationExecution["status"] {
  if (eventType === "automation.execution.completed") return "succeeded";
  if (eventType === "automation.execution.failed") return "failed";
  return "running";
}

function isTerminalStatus(status: AutomationExecution["status"]): boolean {
  return (
    status === "succeeded" || status === "failed" || status === "cancelled"
  );
}

function upsertStep(
  steps: readonly AutomationExecutionStep[],
  step: AutomationExecutionStep,
): AutomationExecutionStep[] {
  const next = steps.filter((existing) => existing.id !== step.id);
  next.push(step);
  return next;
}
