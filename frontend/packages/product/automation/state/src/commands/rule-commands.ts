import type { QueryClient } from "@tanstack/react-query";
import { executeOptimisticCommand } from "@notrelix/query";
import type {
  AutomationRule,
  CreateAutomationRuleInput,
  UpdateAutomationRuleInput,
} from "@notrelix/automation-core";
import type { AutomationRepositories } from "../data/repositories";
import { automationQueryKeys } from "../query/keys";

export async function createAutomationRuleCommand(input: {
  queryClient: QueryClient;
  repositories: AutomationRepositories;
  variables: CreateAutomationRuleInput;
  commandId: string;
}): Promise<AutomationRule> {
  return executeOptimisticCommand({
    queryClient: input.queryClient,
    commandId: input.commandId,
    updates: [],
    variables: input.variables,
    mutationFn: (variables, context) =>
      input.repositories.rules.create({
        ...variables,
        name: variables.name.trim(),
      }),
    invalidate: [automationQueryKeys.rules(input.variables.workspaceId)],
  });
}

export async function updateAutomationRuleCommand(input: {
  queryClient: QueryClient;
  repositories: AutomationRepositories;
  variables: UpdateAutomationRuleInput;
  workspaceId: string;
  commandId: string;
}): Promise<AutomationRule> {
  return executeOptimisticCommand({
    queryClient: input.queryClient,
    commandId: input.commandId,
    updates: [],
    variables: input.variables,
    mutationFn: (variables) => input.repositories.rules.update(variables),
    invalidate: [
      automationQueryKeys.rules(input.workspaceId),
      automationQueryKeys.ruleDetail(input.workspaceId, input.variables.ruleId),
    ],
  });
}

export async function setAutomationRuleEnabledCommand(input: {
  queryClient: QueryClient;
  repositories: AutomationRepositories;
  ruleId: string;
  workspaceId: string;
  enabled: boolean;
  commandId: string;
}): Promise<AutomationRule> {
  return executeOptimisticCommand({
    queryClient: input.queryClient,
    commandId: input.commandId,
    updates: [],
    variables: {
      ruleId: input.ruleId,
      enabled: input.enabled,
    },
    mutationFn: ({ ruleId, enabled }) =>
      enabled
        ? input.repositories.rules.enable(ruleId)
        : input.repositories.rules.disable(ruleId),
    invalidate: [
      automationQueryKeys.rules(input.workspaceId),
      automationQueryKeys.ruleDetail(input.workspaceId, input.ruleId),
    ],
  });
}

export async function deleteAutomationRuleCommand(input: {
  queryClient: QueryClient;
  repositories: AutomationRepositories;
  ruleId: string;
  workspaceId: string;
  commandId: string;
}): Promise<void> {
  return executeOptimisticCommand({
    queryClient: input.queryClient,
    commandId: input.commandId,
    updates: [],
    variables: input.ruleId,
    mutationFn: (ruleId) => input.repositories.rules.delete(ruleId),
    invalidate: [
      automationQueryKeys.rules(input.workspaceId),
      automationQueryKeys.ruleDetail(input.workspaceId, input.ruleId),
    ],
  });
}

export async function testAutomationRuleCommand(input: {
  queryClient: QueryClient;
  repositories: AutomationRepositories;
  ruleId: string;
  workspaceId: string;
  commandId: string;
}) {
  return executeOptimisticCommand({
    queryClient: input.queryClient,
    commandId: input.commandId,
    updates: [],
    variables: input.ruleId,
    mutationFn: (ruleId) => input.repositories.rules.test(ruleId),
    invalidate: [
      automationQueryKeys.executionHistory(input.workspaceId, input.ruleId),
    ],
  });
}
