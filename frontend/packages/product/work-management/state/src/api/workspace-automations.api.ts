import type { NotrelixClient, OperationResponse } from "@notrelix/contracts";
import type {
  CreateWorkspaceAutomationInput,
  WorkspaceAutomationRuleDto,
  WorkspaceAutomationRunDto,
  WorkspaceAutomationRunStatus,
} from "@notrelix/work-management-core";

type RuleRow = NonNullable<OperationResponse<"Automation.Rules.List">>[number];
type RunRow = NonNullable<
  OperationResponse<"Automation.Executions.List">
>[number];

const STATUS_BY_NAME: Readonly<
  Record<NonNullable<RunRow["status"]>, WorkspaceAutomationRunStatus>
> = {
  Queued: "queued",
  Running: "running",
  Succeeded: "succeeded",
  Failed: "failed",
  Cancelled: "cancelled",
};

/** The contract documents the status as its name, which is what the API sends. */
function runStatus(raw: RunRow["status"]): WorkspaceAutomationRunStatus {
  return (raw && STATUS_BY_NAME[raw]) || "queued";
}

function parse(json: string | null | undefined): unknown {
  if (!json) return null;
  try {
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function text(value: unknown): string | null {
  return typeof value === "string" && value !== "" ? value : null;
}

/**
 * A rule's `configuration` is the rule's saved configuration, whose action
 * carries its own JSON text ({ itemId, targetGroupId } for "MoveItem").
 */
function moveTarget(configuration: string | null | undefined): {
  itemId: string | null;
  targetGroupId: string | null;
} {
  const root = parse(configuration) as {
    Action?: { Configuration?: string | null };
  } | null;
  const action = parse(root?.Action?.Configuration) as {
    itemId?: unknown;
    targetGroupId?: unknown;
  } | null;
  return {
    itemId: text(action?.itemId),
    targetGroupId: text(action?.targetGroupId),
  };
}

/** The group a trigger filters on: the saved trigger's own JSON text. */
function triggerGroup(configuration: string | null | undefined): string | null {
  const root = parse(configuration) as {
    Trigger?: { Configuration?: string | null };
  } | null;
  const trigger = parse(root?.Trigger?.Configuration) as {
    groupId?: unknown;
  } | null;
  return text(trigger?.groupId);
}

function toRule(row: RuleRow): WorkspaceAutomationRuleDto {
  const action = row.actionType ?? "";
  const target =
    action === "MoveItem"
      ? moveTarget(row.configuration)
      : { itemId: null, targetGroupId: null };
  return {
    id: row.id ?? "",
    name: row.name ?? "",
    trigger: row.triggerType ?? "",
    action,
    itemId: target.itemId,
    targetGroupId: target.targetGroupId,
    boardId: row.boardId ?? null,
    triggerGroupId: triggerGroup(row.configuration),
    enabled: row.isEnabled === true,
    disabledReason: row.disabledReason ?? null,
    createdAt: row.createdAt ?? "",
  };
}

/** The rules of a workspace and their runs, over the Automation routes. */
export function createWorkspaceAutomationsApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const base = (workspaceId: string) =>
    `/workspaces/${encodeURIComponent(workspaceId)}/automations`;
  return {
    async list(workspaceId: string): Promise<WorkspaceAutomationRuleDto[]> {
      const rows = await api.get<RuleRow[]>(base(workspaceId), quiet);
      return rows.map(toRule);
    },
    create(
      workspaceId: string,
      input: CreateWorkspaceAutomationInput,
    ): Promise<string> {
      return api.post<string>(
        base(workspaceId),
        {
          name: input.name,
          triggerEvent: input.trigger,
          actionType: "MoveItem",
          boardId: input.boardId,
          configuration: JSON.stringify({
            targetGroupId: input.targetGroupId,
          }),
          triggerConfiguration: JSON.stringify(
            input.triggerGroupId ? { groupId: input.triggerGroupId } : {},
          ),
        },
        { idempotencyKey: crypto.randomUUID(), ...quiet },
      );
    },
    setEnabled(
      workspaceId: string,
      ruleId: string,
      enabled: boolean,
    ): Promise<void> {
      return api.patch<void>(
        `${base(workspaceId)}/${encodeURIComponent(ruleId)}/enabled`,
        { isEnabled: enabled },
        quiet,
      );
    },
    async runs(ruleId: string): Promise<WorkspaceAutomationRunDto[]> {
      const rows = await api.get<RunRow[]>(
        `/automations/${encodeURIComponent(ruleId)}/executions?page=1&pageSize=20`,
        quiet,
      );
      return rows.map((row) => ({
        id: row.id ?? "",
        status: runStatus(row.status),
        attempts: row.attemptCount ?? 0,
        error: row.error ?? null,
        createdAt: row.createdAt ?? "",
      }));
    },
  };
}

export type WorkspaceAutomationsApi = ReturnType<
  typeof createWorkspaceAutomationsApi
>;
