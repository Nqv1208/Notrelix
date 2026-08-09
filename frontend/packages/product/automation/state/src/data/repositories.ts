import type {
  AutomationCursorPage,
  AutomationExecution,
  AutomationRule,
  AutomationTemplate,
  CreateAutomationRuleInput,
  UpdateAutomationRuleInput,
} from "@notrelix/automation-core";

export interface AutomationRuleRepository {
  listByWorkspace(workspaceId: string): Promise<AutomationRule[]>;
  getDetail(ruleId: string): Promise<AutomationRule>;
  create(input: CreateAutomationRuleInput): Promise<AutomationRule>;
  update(input: UpdateAutomationRuleInput): Promise<AutomationRule>;
  enable(ruleId: string): Promise<AutomationRule>;
  disable(ruleId: string): Promise<AutomationRule>;
  delete(ruleId: string): Promise<void>;
  test(ruleId: string): Promise<AutomationExecution>;
}

export interface AutomationExecutionRepository {
  listHistory(input: {
    workspaceId: string;
    ruleId?: string;
    cursor?: string;
    limit?: number;
  }): Promise<AutomationCursorPage<AutomationExecution>>;
  getDetail(executionId: string): Promise<AutomationExecution>;
  retry?(executionId: string): Promise<AutomationExecution>;
  cancel?(executionId: string): Promise<AutomationExecution>;
}

export interface AutomationTemplateRepository {
  listTemplates(workspaceId: string): Promise<AutomationTemplate[]>;
}

export interface AutomationRepositories {
  rules: AutomationRuleRepository;
  executions: AutomationExecutionRepository;
  templates: AutomationTemplateRepository;
}
