/**
 * Analytics — the read-only work reports the producer serves (real contracts, not gaps):
 *
 *   Analytics.Reports.GetBoardReport     — GET /boards/:boardId/report
 *   Analytics.Reports.GetWorkspaceReport — GET /workspaces/:workspaceId/work-report
 *
 * Both count the live items of the boards: by status, by assignee, overdue, finished per week for the last
 * the weeks asked for, eight by default and at most 26 (weeks start on Monday, UTC) and the mean time from creation to completion.
 */
import { isoOfMs } from "../../state/clock";
import { defineMockOperation } from "../../operations/types";
import {
  forbidden,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  boardItems,
  personOf,
  type ReportItem,
} from "../../state/mock-reports";
import type { MockStore } from "../../state/mock-store";

const DAY_MS = 86_400_000;
const DEFAULT_WEEKS = 8;
const MAX_WEEKS = 26;

const CATEGORY_OF: Readonly<Record<string, string>> = {
  todo: "Todo",
  doing: "InProgress",
  review: "Review",
  done: "Done",
};
const CATEGORY_ORDER = ["Todo", "InProgress", "Review", "Done"] as const;

function weekStarts(nowMs: number, weeks: number): number[] {
  const days = Math.floor(nowMs / DAY_MS);
  // 1970-01-01 was a Thursday, so days + 3 counts the days since the last Monday.
  const thisMonday = (days - ((days + 3) % 7)) * DAY_MS;
  return Array.from(
    { length: weeks },
    (_, i) => thisMonday - (weeks - 1 - i) * 7 * DAY_MS,
  );
}

function reportOf(
  store: MockStore,
  boardIds: readonly string[],
  weeks: number,
) {
  const nowMs = store.getClock().now().getTime();
  const items: ReportItem[] = boardIds.flatMap((id) => boardItems(store, id));
  const isOpen = (item: ReportItem) => item.completedAt === null;
  const isOverdue = (item: ReportItem) =>
    isOpen(item) && item.dueAt !== null && Date.parse(item.dueAt) < nowMs;

  const single = boardIds.length === 1;
  const byStatus: {
    optionId: string | null;
    name: string | null;
    color: string | null;
    category: string;
    count: number;
  }[] = [];
  if (single) {
    const groups = new Map<string, { category: string; count: number }>();
    for (const item of items) {
      const label = item.statusLabel || "";
      const entry = groups.get(label) ?? {
        category: CATEGORY_OF[item.bucket] ?? "Todo",
        count: 0,
      };
      entry.count += 1;
      groups.set(label, entry);
    }
    for (const [label, entry] of groups) {
      byStatus.push({
        optionId: null,
        name: label || null,
        color: null,
        category: label ? entry.category : "None",
        count: entry.count,
      });
    }
  } else {
    for (const category of CATEGORY_ORDER) {
      byStatus.push({
        optionId: null,
        name: null,
        color: null,
        category,
        count: items.filter((i) => CATEGORY_OF[i.bucket] === category).length,
      });
    }
  }

  const assignees = new Map<
    string | null,
    { items: number; open: number; overdue: number }
  >();
  for (const item of items) {
    const entry = assignees.get(item.assigneeId) ?? {
      items: 0,
      open: 0,
      overdue: 0,
    };
    entry.items += 1;
    if (isOpen(item)) entry.open += 1;
    if (isOverdue(item)) entry.overdue += 1;
    assignees.set(item.assigneeId, entry);
  }
  const byAssignee = [...assignees.entries()]
    .sort(
      ([a, x], [b, y]) =>
        (a === null ? 1 : 0) - (b === null ? 1 : 0) || y.items - x.items,
    )
    .map(([userId, entry]) => ({
      userId,
      name: userId ? (personOf(store, userId)?.name ?? "Unknown") : null,
      ...entry,
    }));

  const starts = weekStarts(nowMs, weeks);
  const completedPerWeek = starts.map((start) => ({
    weekStart: isoOfMs(start),
    completed: items.filter((i) => {
      if (!i.completedAt) return false;
      const at = Date.parse(i.completedAt);
      return at >= start && at < start + 7 * DAY_MS && at <= nowMs;
    }).length,
  }));

  const durations = items
    .filter((i) => i.completedAt)
    .map(
      (i) =>
        (Date.parse(i.completedAt ?? "") -
          Date.parse(store.getCard(i.id)?.createdAt ?? i.completedAt ?? "")) /
        1000,
    )
    .filter((seconds) => Number.isFinite(seconds));

  return {
    total: items.length,
    open: items.filter(isOpen).length,
    done: items.filter((i) => !isOpen(i)).length,
    overdue: items.filter(isOverdue).length,
    byStatus,
    byAssignee,
    completedPerWeek,
    averageCompletionSeconds: durations.length
      ? durations.reduce((a, b) => a + b, 0) / durations.length
      : null,
  };
}

/** The period asked for (1 to 26 weeks, eight by default), or null when it is out of range. */
function weeksOf(raw: string | undefined): number | null {
  if (raw === undefined) return DEFAULT_WEEKS;
  const weeks = Number(raw);
  return Number.isInteger(weeks) && weeks >= 1 && weeks <= MAX_WEEKS
    ? weeks
    : null;
}

type BoardParams = { boardId: string };
type WorkspaceParams = { workspaceId: string };

export const workReportOperations = [
  defineMockOperation<BoardParams, never, unknown>({
    id: "analytics.boardReport",
    contract: {
      kind: "openapi",
      operationId: "Analytics.Reports.GetBoardReport",
    },
    method: "GET",
    route: "/boards/:boardId/report",
    async handle({ params, query, store }) {
      const board = store.getBoard(params.boardId);
      if (!board) return notFound("Board not found");
      if (!store.getCurrentMemberRole(board.workspaceId)) return forbidden();
      const weeks = weeksOf(query.weeks);
      if (weeks === null) return validationError({ Weeks: ["Out of range."] });
      return ok({
        boardId: board.id,
        generatedAt: store.getClock().now().toISOString(),
        report: reportOf(store, [board.id], weeks),
      });
    },
  }),

  defineMockOperation<WorkspaceParams, never, unknown>({
    id: "analytics.workspaceReport",
    contract: {
      kind: "openapi",
      operationId: "Analytics.Reports.GetWorkspaceReport",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/work-report",
    async handle({ params, query, store }) {
      if (!store.getWorkspace(params.workspaceId))
        return notFound("Workspace not found");
      const role = store.getCurrentMemberRole(params.workspaceId);
      if (!role) return forbidden();
      const weeks = weeksOf(query.weeks);
      if (weeks === null) return validationError({ Weeks: ["Out of range."] });
      const spaceId = query.spaceId ?? null;
      if (spaceId && role !== "owner" && role !== "admin") return forbidden();
      const boards = store
        .getBoards(params.workspaceId)
        .filter(
          (board) =>
            !board.isArchived &&
            (spaceId === null || board.spaceId === spaceId),
        );
      return ok({
        generatedAt: store.getClock().now().toISOString(),
        boardCount: boards.length,
        spaceId,
        report: reportOf(
          store,
          boards.map((b) => b.id),
          weeks,
        ),
      });
    },
  }),
];
