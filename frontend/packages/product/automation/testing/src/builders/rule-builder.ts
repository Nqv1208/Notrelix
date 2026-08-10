import type { AutomationRule } from "@notrelix/automation-core";

export function buildAutomationRule(
  overrides: Partial<AutomationRule> = {},
): AutomationRule {
  return {
    id: "rule-1",
    workspaceId: "workspace-1",
    boardId: "board-1",
    name: "Archive completed work",
    description: "Archive cards when their status changes to Done.",
    triggerType: "card_status_changed",
    triggerConfig: { to: "done" },
    actionType: "archive_card",
    actionConfig: {},
    isEnabled: true,
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}
