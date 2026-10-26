/**
 * The automation rules of the ERP board and their run history, as the mock
 * world starts: two rules that are on, with runs over the days before the
 * mock's "now" (Monday 12/10/2026 10:40 in Hanoi). Fixed ids and instants, so
 * every seeded world is the same. Most runs succeeded, each with the result of
 * every action; one stopped on its second action with a reason a person can
 * read. A rule edited once is at version 2 and its older runs say version 1.
 */

import { mockIds } from "./mock-ids";
import { ERP_GROUPS, ERP_PEOPLE, ERP_STATUS_OPTIONS } from "./mock-erp-board";
import type {
  MockAutomationExecution,
  MockAutomationRule,
} from "./mock-workspace-automations";

const workspaceId = mockIds.workspaces.primary;
const boardId = mockIds.boards.erp;

const testGroup = ERP_GROUPS.find((group) => group.key === "test")!.id;
const dataGroup = ERP_GROUPS.find((group) => group.key === "data")!.id;
const status = (name: string) =>
  ERP_STATUS_OPTIONS.find((option) => option.name === name)!.id;

const REVIEW_RULE_ID = "mock-automation-erp-review";
const DATA_RULE_ID = "mock-automation-erp-data";

const notifyReview = JSON.stringify({
  userId: ERP_PEOPLE.HA.id,
  message: "Ready for approval",
});
const setReview = JSON.stringify({ optionId: status("Chờ duyệt") });
const setTodo = JSON.stringify({ optionId: status("Cần làm") });
const notifyNew = JSON.stringify({
  userId: ERP_PEOPLE.HA.id,
  message: "A new data item to triage",
});

function rule(
  input: Pick<
    MockAutomationRule,
    | "id"
    | "name"
    | "triggerType"
    | "triggerConfiguration"
    | "conditions"
    | "actions"
    | "version"
    | "createdAt"
    | "updatedAt"
  >,
): MockAutomationRule {
  const [first, ...more] = input.actions;
  return {
    ...input,
    workspaceId,
    boardId,
    actionType: first?.type ?? "",
    configuration: JSON.stringify({
      Trigger: {
        Type: input.triggerType,
        Configuration: input.triggerConfiguration,
        SchemaVersion: 1,
      },
      Action: {
        Type: first?.type ?? "",
        Configuration: first?.configuration ?? null,
        SchemaVersion: 1,
      },
      MoreActions: more.map((action) => ({
        Type: action.type,
        Configuration: action.configuration,
        SchemaVersion: 1,
      })),
      Condition: input.conditions
        ? { RawJson: input.conditions, SchemaVersion: 1 }
        : null,
      SchemaVersion: 1,
    }),
    isEnabled: true,
    executorUserId: ERP_PEOPLE.HP.id,
    disabledReason: null,
    removed: false,
  };
}

export const ERP_AUTOMATION_RULES: MockAutomationRule[] = [
  rule({
    id: REVIEW_RULE_ID,
    name: "Hand a tested item to the approver",
    triggerType: "ItemMovedToGroup",
    triggerConfiguration: JSON.stringify({ groupId: testGroup }),
    conditions: JSON.stringify({
      all: [{ type: "status", optionId: status("Đang làm") }],
    }),
    actions: [
      { type: "ChangeStatus", configuration: setReview },
      { type: "NotifyMember", configuration: notifyReview },
    ],
    version: 2,
    createdAt: "2026-10-05T03:10:00.000Z",
    updatedAt: "2026-10-08T05:20:00.000Z",
  }),
  rule({
    id: DATA_RULE_ID,
    name: "Triage new Data items",
    triggerType: "ItemCreated",
    triggerConfiguration: null,
    conditions: JSON.stringify({
      all: [{ type: "group", groupId: dataGroup }],
    }),
    actions: [
      { type: "ChangeStatus", configuration: setTodo },
      { type: "NotifyMember", configuration: notifyNew },
    ],
    version: 1,
    createdAt: "2026-10-06T02:30:00.000Z",
    updatedAt: null,
  }),
];

type Outcome = "ok" | "notify-failed";

function run(
  id: string,
  ruleId: string,
  createdAt: string,
  ruleVersion: number,
  itemId: string,
  types: [string, string],
  outcome: Outcome = "ok",
): MockAutomationExecution {
  const failed = outcome === "notify-failed";
  return {
    id,
    workspaceId,
    ruleId,
    status: failed ? "Failed" : "Succeeded",
    attemptCount: failed ? 3 : 1,
    payload: JSON.stringify({ itemId }),
    error: failed ? "Stopped at action 2 after 3 attempts." : null,
    createdAt,
    ruleVersion,
    steps: [
      { index: 0, type: types[0], status: "Succeeded", error: null },
      {
        index: 1,
        type: types[1],
        status: failed ? "Failed" : "Succeeded",
        error: failed
          ? "Nguyễn Thu Hà can no longer see this Board, so there was no one to notify."
          : null,
      },
    ],
  };
}

const REVIEW: [string, string] = ["ChangeStatus", "NotifyMember"];
const DATA: [string, string] = ["ChangeStatus", "NotifyMember"];
const item = (n: number) => `mock-erp-item-${n}`;

export const ERP_AUTOMATION_EXECUTIONS: MockAutomationExecution[] = [
  run(
    "mock-execution-review-1",
    REVIEW_RULE_ID,
    "2026-10-12T02:52:00.000Z",
    2,
    item(144),
    REVIEW,
  ),
  run(
    "mock-execution-review-2",
    REVIEW_RULE_ID,
    "2026-10-10T11:05:00.000Z",
    2,
    item(146),
    REVIEW,
    "notify-failed",
  ),
  run(
    "mock-execution-review-3",
    REVIEW_RULE_ID,
    "2026-10-09T09:21:00.000Z",
    2,
    item(142),
    REVIEW,
  ),
  run(
    "mock-execution-review-5",
    REVIEW_RULE_ID,
    "2026-10-07T03:12:00.000Z",
    1,
    item(131),
    REVIEW,
  ),
  run(
    "mock-execution-data-1",
    DATA_RULE_ID,
    "2026-10-11T04:15:00.000Z",
    1,
    item(152),
    DATA,
  ),
  run(
    "mock-execution-data-2",
    DATA_RULE_ID,
    "2026-10-09T02:40:00.000Z",
    1,
    item(153),
    DATA,
  ),
  run(
    "mock-execution-data-3",
    DATA_RULE_ID,
    "2026-10-07T01:58:00.000Z",
    1,
    item(149),
    DATA,
  ),
];
