import { workspaceQueryKey } from "@notrelix/query";

export const automationQueryKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "automation"),
  rules: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "automation", "rules"),
  ruleDetail: (workspaceId: string, ruleId: string) =>
    workspaceQueryKey(workspaceId, "automation", "rules", ruleId),
  executionHistory: (workspaceId: string, ruleId?: string) =>
    ruleId
      ? workspaceQueryKey(
          workspaceId,
          "automation",
          "executions",
          "rule",
          ruleId,
        )
      : workspaceQueryKey(workspaceId, "automation", "executions"),
  executionDetail: (workspaceId: string, executionId: string) =>
    workspaceQueryKey(workspaceId, "automation", "executions", executionId),
  templates: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "automation", "templates"),
} as const;
