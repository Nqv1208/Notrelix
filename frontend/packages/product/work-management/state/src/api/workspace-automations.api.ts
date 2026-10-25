import type { NotrelixClient, OperationResponse } from "@notrelix/contracts";
import type {
  AutomationActionInput,
  AutomationConditionInput,
  CreateWorkspaceAutomationInput,
  WorkspaceAutomationDefinition,
  WorkspaceAutomationRuleDto,
  WorkspaceAutomationRunDto,
  WorkspaceAutomationRunStatus,
} from "@notrelix/work-management-core";

type RuleRow = NonNullable<OperationResponse<"Automation.Rules.List">>[number];
type ActionRow = NonNullable<RuleRow["actions"]>[number];
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

function toAction(row: ActionRow): AutomationActionInput | null {
  const config = (parse(row.configuration) ?? {}) as Record<string, unknown>;
  switch (row.type) {
    case "MoveItem":
      return text(config.targetGroupId)
        ? { type: "MoveItem", groupId: text(config.targetGroupId)! }
        : null;
    case "ChangeStatus":
      return text(config.optionId)
        ? { type: "ChangeStatus", optionId: text(config.optionId)! }
        : null;
    case "AssignPerson":
      return text(config.userId)
        ? { type: "AssignPerson", userId: text(config.userId)! }
        : null;
    case "UpdateField":
      return text(config.fieldId)
        ? {
            type: "UpdateField",
            fieldId: text(config.fieldId)!,
            value: config.value == null ? "" : String(config.value),
          }
        : null;
    case "NotifyMember":
      return text(config.userId)
        ? {
            type: "NotifyMember",
            userId: text(config.userId)!,
            message: text(config.message) ?? "",
          }
        : null;
    case "SendEmail":
      return text(config.userId)
        ? {
            type: "SendEmail",
            userId: text(config.userId)!,
            subject: text(config.subject) ?? "",
            body: text(config.body) ?? "",
          }
        : null;
    default:
      return null;
  }
}

function toConditions(
  json: string | null | undefined,
): AutomationConditionInput[] {
  const root = parse(json) as { all?: Record<string, unknown>[] } | null;
  const out: AutomationConditionInput[] = [];
  for (const row of root?.all ?? []) {
    if (row.type === "group" && text(row.groupId))
      out.push({ type: "group", groupId: text(row.groupId)! });
    else if (row.type === "status" && text(row.optionId))
      out.push({ type: "status", optionId: text(row.optionId)! });
    else if (row.type === "field" && text(row.fieldId))
      out.push({
        type: "field",
        fieldId: text(row.fieldId)!,
        value: row.value == null ? "" : String(row.value),
      });
  }
  return out;
}

function toRule(row: RuleRow): WorkspaceAutomationRuleDto {
  return {
    id: row.id ?? "",
    name: row.name ?? "",
    trigger: row.triggerType ?? "",
    actions: (row.actions ?? [])
      .map(toAction)
      .filter((action): action is AutomationActionInput => action !== null),
    conditions: toConditions(row.conditions),
    boardId: row.boardId ?? null,
    triggerGroupId: triggerGroup(row.configuration),
    enabled: row.isEnabled === true,
    disabledReason: row.disabledReason ?? null,
    version: row.version ?? 1,
    createdAt: row.createdAt ?? "",
  };
}

/** A number-looking value is sent as a number, anything else as text. */
function fieldValue(value: string): unknown {
  const trimmed = value.trim();
  return trimmed !== "" && Number.isFinite(Number(trimmed))
    ? Number(trimmed)
    : value;
}

function actionBody(action: AutomationActionInput) {
  switch (action.type) {
    case "MoveItem":
      return {
        type: action.type,
        configuration: json({ targetGroupId: action.groupId }),
      };
    case "ChangeStatus":
      return {
        type: action.type,
        configuration: json({ optionId: action.optionId }),
      };
    case "AssignPerson":
      return {
        type: action.type,
        configuration: json({ userId: action.userId }),
      };
    case "UpdateField":
      return {
        type: action.type,
        configuration: json({
          fieldId: action.fieldId,
          value: fieldValue(action.value),
        }),
      };
    case "NotifyMember":
      return {
        type: action.type,
        configuration: json({
          userId: action.userId,
          ...(action.message.trim() ? { message: action.message.trim() } : {}),
        }),
      };
    case "SendEmail":
      return {
        type: action.type,
        configuration: json({
          userId: action.userId,
          subject: action.subject.trim(),
          ...(action.body.trim() ? { body: action.body.trim() } : {}),
        }),
      };
  }
}

const json = (value: unknown) => JSON.stringify(value);

function conditionsBody(
  conditions: readonly AutomationConditionInput[],
): string | undefined {
  if (conditions.length === 0) return undefined;
  return json({
    all: conditions.map((condition) =>
      condition.type === "group"
        ? { type: "group", groupId: condition.groupId }
        : condition.type === "status"
          ? { type: "status", optionId: condition.optionId }
          : {
              type: "field",
              fieldId: condition.fieldId,
              value: fieldValue(condition.value),
            },
    ),
  });
}

function definitionBody(definition: WorkspaceAutomationDefinition) {
  return {
    name: definition.name,
    triggerEvent: definition.trigger,
    triggerConfiguration: json(
      definition.triggerGroupId ? { groupId: definition.triggerGroupId } : {},
    ),
    conditions: conditionsBody(definition.conditions),
    actions: definition.actions.map(actionBody),
  };
}

/** The rules of a workspace and their runs, over the Automation routes. */
export function createWorkspaceAutomationsApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const base = (workspaceId: string) =>
    `/workspaces/${encodeURIComponent(workspaceId)}/automations`;
  const claim = (boardId: string) => `boardId=${encodeURIComponent(boardId)}`;
  return {
    /** The rules of one board (the caller manages that board). */
    async list(
      workspaceId: string,
      boardId: string,
    ): Promise<WorkspaceAutomationRuleDto[]> {
      const rows = await api.get<RuleRow[]>(
        `${base(workspaceId)}?${claim(boardId)}`,
        quiet,
      );
      return rows.map(toRule);
    },
    create(
      workspaceId: string,
      input: CreateWorkspaceAutomationInput,
    ): Promise<string> {
      return api.post<string>(
        base(workspaceId),
        { ...definitionBody(input), boardId: input.boardId },
        { idempotencyKey: crypto.randomUUID(), ...quiet },
      );
    },
    /** Edits the rule: the backend keeps the old version for runs already started. */
    update(
      workspaceId: string,
      boardId: string,
      ruleId: string,
      definition: WorkspaceAutomationDefinition,
    ): Promise<void> {
      return api.put<void>(
        `${base(workspaceId)}/${encodeURIComponent(ruleId)}`,
        { ...definitionBody(definition), boardId },
        { idempotencyKey: crypto.randomUUID(), ...quiet },
      );
    },
    remove(
      workspaceId: string,
      boardId: string,
      ruleId: string,
    ): Promise<void> {
      return api.delete<void>(
        `${base(workspaceId)}/${encodeURIComponent(ruleId)}?${claim(boardId)}`,
        { idempotencyKey: crypto.randomUUID(), ...quiet },
      );
    },
    setEnabled(
      workspaceId: string,
      boardId: string,
      ruleId: string,
      enabled: boolean,
    ): Promise<void> {
      return api.patch<void>(
        `${base(workspaceId)}/${encodeURIComponent(ruleId)}/enabled`,
        { isEnabled: enabled, boardId },
        quiet,
      );
    },
    async runs(
      boardId: string,
      ruleId: string,
    ): Promise<WorkspaceAutomationRunDto[]> {
      const rows = await api.get<RunRow[]>(
        `/automations/${encodeURIComponent(ruleId)}/executions?page=1&pageSize=20&${claim(boardId)}`,
        quiet,
      );
      return rows.map((row) => ({
        id: row.id ?? "",
        status: runStatus(row.status),
        attempts: row.attemptCount ?? 0,
        error: row.error ?? null,
        ruleVersion: row.ruleVersion ?? null,
        steps: (row.steps ?? []).map((step) => ({
          index: step.index ?? 0,
          type: step.type ?? "",
          status: step.status === "Succeeded" ? "succeeded" : "failed",
          error: step.error ?? null,
        })),
        createdAt: row.createdAt ?? "",
      }));
    },
  };
}

export type WorkspaceAutomationsApi = ReturnType<
  typeof createWorkspaceAutomationsApi
>;
