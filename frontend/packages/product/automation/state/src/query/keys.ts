export const automationQueryKeys = {
  all: ['automation'] as const,
  workspace: (workspaceId: string) =>
    [...automationQueryKeys.all, 'workspace', workspaceId] as const,
  rules: (workspaceId: string) =>
    [...automationQueryKeys.workspace(workspaceId), 'rules'] as const,
  ruleDetail: (ruleId: string) =>
    [...automationQueryKeys.all, 'rules', ruleId] as const,
  executionHistory: (workspaceId: string, ruleId?: string) =>
    [...automationQueryKeys.workspace(workspaceId), 'executions', { ruleId: ruleId ?? null }] as const,
  executionDetail: (executionId: string) =>
    [...automationQueryKeys.all, 'executions', executionId] as const,
  templates: (workspaceId: string) =>
    [...automationQueryKeys.workspace(workspaceId), 'templates'] as const,
};
