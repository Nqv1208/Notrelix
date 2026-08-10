import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import type { AutomationRepositories } from "../data/repositories";
import { automationQueryKeys } from "./keys";

export function useAutomationRules(
  repositories: AutomationRepositories,
  workspaceId: string,
) {
  return useQuery({
    queryKey: automationQueryKeys.rules(workspaceId),
    queryFn: () => repositories.rules.listByWorkspace(workspaceId),
    enabled: workspaceId.length > 0,
  });
}

export function useAutomationRuleDetail(
  repositories: AutomationRepositories,
  workspaceId: string,
  ruleId: string,
) {
  return useQuery({
    queryKey: automationQueryKeys.ruleDetail(workspaceId, ruleId),
    queryFn: () => repositories.rules.getDetail(ruleId),
    enabled: workspaceId.length > 0 && ruleId.length > 0,
  });
}

export function useAutomationExecutionHistory(
  repositories: AutomationRepositories,
  input: { workspaceId: string; ruleId?: string; limit?: number },
) {
  return useInfiniteQuery({
    queryKey: automationQueryKeys.executionHistory(
      input.workspaceId,
      input.ruleId,
    ),
    queryFn: ({ pageParam }) =>
      repositories.executions.listHistory({
        workspaceId: input.workspaceId,
        ruleId: input.ruleId,
        cursor: pageParam,
        limit: input.limit,
      }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.nextCursor,
    enabled: input.workspaceId.length > 0,
  });
}

export function useAutomationExecutionDetail(
  repositories: AutomationRepositories,
  workspaceId: string,
  executionId: string,
) {
  return useQuery({
    queryKey: automationQueryKeys.executionDetail(workspaceId, executionId),
    queryFn: () => repositories.executions.getDetail(executionId),
    enabled: workspaceId.length > 0 && executionId.length > 0,
  });
}

export function useAutomationTemplates(
  repositories: AutomationRepositories,
  workspaceId: string,
) {
  return useQuery({
    queryKey: automationQueryKeys.templates(workspaceId),
    queryFn: () => repositories.templates.listTemplates(workspaceId),
    enabled: workspaceId.length > 0,
  });
}
