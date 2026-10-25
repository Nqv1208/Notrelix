import type { WorkspaceRulesPanelProps } from "../components/board-settings/board-settings-types";

/**
 * Mockup world of the workspace automation rules in the board settings, for
 * stories and tests only. Deterministic: every date is a literal.
 */
const noop = () => undefined;
const written = () => Promise.resolve({ ok: true } as const);

const ITEMS = [
  { id: "i-1", name: "Chốt phạm vi ERP", groupId: "g-todo" },
  { id: "i-2", name: "Khảo sát kho Hải Phòng", groupId: "g-todo" },
  { id: "i-3", name: "Nghiệm thu giai đoạn 1", groupId: "g-done" },
];
const GROUPS = [
  { id: "g-todo", name: "Cần làm" },
  { id: "g-done", name: "Hoàn thành" },
];

export function workspaceRulesPanel(
  overrides: Partial<WorkspaceRulesPanelProps> = {},
): WorkspaceRulesPanelProps {
  return {
    boardId: "b-1",
    status: "ready",
    rules: [
      {
        id: "r-1",
        name: "Cất việc chốt phạm vi khi có việc mới",
        trigger: "ItemMovedToGroup",
        action: "MoveItem",
        itemId: null,
        targetGroupId: "g-done",
        boardId: "b-1",
        triggerGroupId: "g-todo",
        enabled: true,
        disabledReason: null,
        createdAt: "2026-10-12T03:40:00.000Z",
      },
      {
        id: "r-2",
        name: "Gọi webhook khi giao việc",
        trigger: "ItemAssigned",
        action: "Webhook",
        itemId: null,
        targetGroupId: null,
        boardId: "b-1",
        triggerGroupId: null,
        enabled: false,
        disabledReason: "Disabled after 5 consecutive failed executions.",
        createdAt: "2026-10-11T03:40:00.000Z",
      },
    ],
    items: ITEMS,
    groups: GROUPS,
    onReload: noop,
    onCreate: written,
    onSetEnabled: written,
    openRuleId: null,
    onOpenRuns: noop,
    runs: { status: "ready", data: [] },
    ...overrides,
  };
}

export const WORKSPACE_RULE_RUNS: WorkspaceRulesPanelProps["runs"] = {
  status: "ready",
  data: [
    {
      id: "x-2",
      status: "failed",
      attempts: 3,
      error: "The target group was deleted.",
      createdAt: "2026-10-12T04:10:00.000Z",
    },
    {
      id: "x-1",
      status: "succeeded",
      attempts: 1,
      error: null,
      createdAt: "2026-10-12T03:50:00.000Z",
    },
  ],
};
