import type {
  AutomationCursorPage,
  AutomationExecution,
  AutomationRule,
  AutomationTemplate,
  CreateAutomationRuleInput,
  UpdateAutomationRuleInput,
} from "@notrelix/automation-core";
import type { AutomationRepositories } from "@notrelix/automation-state";
import { buildAutomationRule } from "../builders/rule-builder";

export function createFakeAutomationRepositories(
  input: {
    rules?: AutomationRule[];
    executions?: AutomationExecution[];
    templates?: AutomationTemplate[];
  } = {},
): AutomationRepositories {
  const rules = new Map<string, AutomationRule>(
    (input.rules ?? [buildAutomationRule()]).map((rule) => [rule.id, rule]),
  );
  const executions = new Map<string, AutomationExecution>(
    (input.executions ?? []).map((execution) => [execution.id, execution]),
  );
  const templates = input.templates ?? [];

  return {
    rules: {
      async listByWorkspace(workspaceId: string) {
        return [...rules.values()].filter(
          (rule) => rule.workspaceId === workspaceId,
        );
      },
      async getDetail(ruleId: string) {
        return requireRule(rules, ruleId);
      },
      async create(input: CreateAutomationRuleInput) {
        const rule = buildAutomationRule({
          id: `rule-${rules.size + 1}`,
          workspaceId: input.workspaceId,
          boardId: input.boardId,
          name: input.name,
          description: input.description ?? "",
          triggerType: input.triggerType,
          triggerConfig: input.triggerConfig,
          actionType: input.actionType,
          actionConfig: input.actionConfig,
        });
        rules.set(rule.id, rule);
        return rule;
      },
      async update(input: UpdateAutomationRuleInput) {
        const current = requireRule(rules, input.ruleId);
        const next: AutomationRule = {
          ...current,
          name: input.name ?? current.name,
          description: input.description ?? current.description,
          triggerConfig: input.triggerConfig ?? current.triggerConfig,
          actionConfig: input.actionConfig ?? current.actionConfig,
        };
        rules.set(next.id, next);
        return next;
      },
      async enable(ruleId: string) {
        return setEnabled(rules, ruleId, true);
      },
      async disable(ruleId: string) {
        return setEnabled(rules, ruleId, false);
      },
      async delete(ruleId: string) {
        rules.delete(ruleId);
      },
      async test(ruleId: string) {
        const rule = requireRule(rules, ruleId);
        const execution: AutomationExecution = {
          id: `execution-${executions.size + 1}`,
          ruleId,
          workspaceId: rule.workspaceId,
          boardId: rule.boardId,
          status: "queued",
          triggeredBy: "test",
          startedAt: "2026-01-01T00:00:00.000Z",
          steps: [],
        };
        executions.set(execution.id, execution);
        return execution;
      },
    },
    executions: {
      async listHistory({
        workspaceId,
        ruleId,
      }): Promise<AutomationCursorPage<AutomationExecution>> {
        return {
          items: [...executions.values()].filter(
            (execution) =>
              execution.workspaceId === workspaceId &&
              (ruleId === undefined || execution.ruleId === ruleId),
          ),
        };
      },
      async getDetail(executionId: string) {
        const execution = executions.get(executionId);
        if (!execution) {
          throw new Error(`Automation execution not found: ${executionId}`);
        }
        return execution;
      },
    },
    templates: {
      async listTemplates() {
        return templates;
      },
    },
  };
}

function requireRule(
  rules: Map<string, AutomationRule>,
  ruleId: string,
): AutomationRule {
  const rule = rules.get(ruleId);
  if (!rule) {
    throw new Error(`Automation rule not found: ${ruleId}`);
  }
  return rule;
}

async function setEnabled(
  rules: Map<string, AutomationRule>,
  ruleId: string,
  enabled: boolean,
): Promise<AutomationRule> {
  const current = requireRule(rules, ruleId);
  const next = { ...current, isEnabled: enabled };
  rules.set(ruleId, next);
  return next;
}
