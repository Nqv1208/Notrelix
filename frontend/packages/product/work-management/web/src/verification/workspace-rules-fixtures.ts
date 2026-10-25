import type { WorkspaceRulesPanelProps } from "../components/board-settings/board-settings-types";

/**
 * Mockup world of the workspace automation rules in the board settings, for
 * stories and tests only. Deterministic: every date is a literal.
 */
const noop = () => undefined;
const written = () => Promise.resolve({ ok: true } as const);

const GROUPS = [
  { id: "g-todo", name: "Cần làm" },
  { id: "g-done", name: "Hoàn thành" },
];
const STATUSES = [
  { id: "s-open", name: "Đang làm" },
  { id: "s-review", name: "Chờ duyệt" },
];
const PEOPLE = [
  { id: "u-lan", name: "Lan Phạm" },
  { id: "u-minh", name: "Minh Trần" },
];
const FIELDS = [
  { id: "f-note", name: "Ghi chú" },
  { id: "f-points", name: "Điểm" },
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
        name: "Chuyển việc xong cho người duyệt",
        trigger: "ItemMovedToGroup",
        actions: [
          { type: "ChangeStatus", optionId: "s-review" },
          { type: "AssignPerson", userId: "u-lan" },
          {
            type: "NotifyMember",
            userId: "u-lan",
            message: "Có việc cần duyệt",
          },
        ],
        conditions: [{ type: "status", optionId: "s-open" }],
        boardId: "b-1",
        triggerGroupId: "g-done",
        enabled: true,
        disabledReason: null,
        version: 3,
        createdAt: "2026-10-12T03:40:00.000Z",
      },
      {
        id: "r-2",
        name: "Báo khi giao việc",
        trigger: "ItemAssigned",
        actions: [
          {
            type: "SendEmail",
            userId: "u-minh",
            subject: "Việc mới",
            body: "",
          },
        ],
        conditions: [],
        boardId: "b-1",
        triggerGroupId: null,
        enabled: false,
        disabledReason: "Disabled after 5 consecutive failed executions.",
        version: 1,
        createdAt: "2026-10-11T03:40:00.000Z",
      },
    ],
    groups: GROUPS,
    statuses: STATUSES,
    people: PEOPLE,
    fields: FIELDS,
    onReload: noop,
    onCreate: written,
    onUpdate: written,
    onDelete: written,
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
      error: "Action 2 (AssignPerson): Target action business rejection.",
      ruleVersion: 2,
      steps: [
        { index: 0, type: "ChangeStatus", status: "succeeded", error: null },
        {
          index: 1,
          type: "AssignPerson",
          status: "failed",
          error: "Target action business rejection.",
        },
      ],
      createdAt: "2026-10-12T04:10:00.000Z",
    },
    {
      id: "x-1",
      status: "succeeded",
      attempts: 1,
      error: null,
      ruleVersion: 3,
      steps: [],
      createdAt: "2026-10-12T03:50:00.000Z",
    },
  ],
};
