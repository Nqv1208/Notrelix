/**
 * What the Reports pages of the 2026-10 Reports mockup need (gap
 * `CTR-GAP-REPORTS`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Reports"): the reports a person sees, the widgets of a
 * report computed from the boards they may see, the drill-down list behind a
 * widget row, and the export jobs. A report is read-only over the boards: it
 * never changes an item.
 *
 * The widgets are computed from the board's own items every time they are
 * read (so an item completed on the board moves the report). Two things the
 * board seed does not hold are fixtures of the analytics read model: the
 * weekly completion history of the ERP board (the mockup draws 2 + 2 + 1;
 * the seed's last-completion dates of its five finished items fall 1 + 1 + 3)
 * and the ERP item to "Phân hệ" mapping.
 */
import { isoOfMs, utcWeekdayOfMs } from "./clock";
import { hanoiInstant } from "./mock-erp-board";
import { mockIds } from "./mock-ids";
import type { MockStore } from "./mock-store";

export type ReportBucket = "todo" | "doing" | "review" | "done";
export type ReportPriority = "urgent" | "high" | "medium" | "low";
export type ReportTone =
  "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";

export interface ReportPerson {
  id: string;
  name: string;
  tone: ReportTone;
}

export interface ReportScope {
  id: string;
  label: string;
  tone: ReportTone;
}

export type ReportAccess = "edit" | "view";
export type WidgetSize = "wide" | "half" | "side";
export type WidgetKind =
  | "status"
  | "velocity"
  | "module"
  | "priority"
  | "due"
  | "workload"
  | "dept-done"
  | "dept-overdue"
  | "note"
  | "erp-progress"
  | "number"
  | "columns"
  | "line"
  | "donut"
  | "table"
  | "progress";
export type GroupBy = "assignee" | "status" | "priority" | "module" | "week";
export type Metric = "count" | "overdue";
export type FilterId =
  "not-done" | "overdue" | "assigned" | "unassigned" | "high-priority";

export const FILTER_IDS: readonly FilterId[] = [
  "not-done",
  "overdue",
  "assigned",
  "unassigned",
  "high-priority",
];

/** What is stored for one widget of a report; its numbers are never stored. */
export interface WidgetSpec {
  id: string;
  kind: WidgetKind;
  size: WidgetSize;
  /** A title the person typed; else the title the kind has by default. */
  title?: string | null;
  /** The title of a widget a layout placed (the client words it). */
  titleKey?: string | null;
  boardId?: string | null;
  metric?: Metric;
  groupBy?: GroupBy;
  filters?: FilterId[];
  text?: string | null;
  /** Who wrote a note widget, and the day ("YYYY-MM-DD"). */
  authorId?: string | null;
  writtenOn?: string | null;
}

export interface StoredReport {
  id: string;
  /** null = a report nobody named yet. */
  name: string | null;
  scopeId: string;
  scopeLabel: string;
  scopeTone: ReportTone;
  ownerId: string;
  sharedWith: string[];
  includesViewer: boolean;
  editorIds: string[];
  createdAt: string;
  updatedAt: string;
  dataAsOf: string;
  /** The board the report counts; null = many boards or none yet. */
  sourceBoardId: string | null;
  multiSource: boolean;
  excludedBoardCount: number;
  widgetCountShown: boolean;
  widgets: WidgetSpec[];
  /** A widget the mock recomputes: when true the stale one is fresh. */
  recomputed: boolean;
}

export const REPORT_IDS = {
  erp: "mock-report-erp",
  close: "mock-report-close",
  weekly: "mock-report-weekly",
} as const;

export const REPORTS_TIME_ZONE = "Asia/Ho_Chi_Minh";
/** The ERP board's go-live (the mockups' "01/11"). */
const ERP_LAUNCH_DAY = "2026-11-01";

// ── People and departments ─────────────────────────────────────────────

const PERSON_TONE: Readonly<Record<string, ReportTone>> = {
  "mock-user-erp-quan": "blue",
  "mock-user-erp-ngoc-anh": "violet",
  "mock-user-erp-vu-thanh": "sage",
  "mock-user-erp-phuc": "coral",
  "mock-user-erp-thanh-tam": "sage",
  "mock-user-commenter": "gold",
  [mockIds.users.owner]: "sage",
};

/** Order of people with the same number of items (the mockup's rows). */
const PERSON_ORDER: readonly string[] = [
  "mock-user-erp-quan",
  "mock-user-erp-ngoc-anh",
  "mock-user-erp-vu-thanh",
  "mock-user-erp-phuc",
  "mock-user-erp-thanh-tam",
  mockIds.users.commenter,
  mockIds.users.owner,
];

const EXEC_AUTHOR: ReportPerson = {
  id: "mock-person-quoc-hung",
  name: "Trịnh Quốc Hưng",
  tone: "crimson",
};
const ERP_CREATOR_ID = "mock-user-erp-phuc";

export function personOf(
  store: MockStore,
  userId: string,
): ReportPerson | null {
  if (userId === EXEC_AUTHOR.id) return EXEC_AUTHOR;
  const user = store.getUser(userId);
  if (!user) return null;
  return {
    id: user.id,
    name: user.name,
    tone: PERSON_TONE[user.id] ?? "sage",
  };
}

const SCOPES = {
  tech: {
    id: mockIds.spaces.engineering,
    label: "Phòng Công nghệ",
    tone: "blue",
  },
  accounting: {
    id: mockIds.spaces.accounting,
    label: "Phòng Kế toán",
    tone: "gold",
  },
  executive: {
    id: mockIds.spaces.executive,
    label: "Ban giám đốc",
    tone: "crimson",
  },
} as const satisfies Record<string, ReportScope>;

/** The departments a report can be shared with. */
export const SHARE_TARGETS: readonly ReportScope[] = [
  SCOPES.tech,
  SCOPES.accounting,
  SCOPES.executive,
  { id: mockIds.spaces.marketing, label: "Phòng Marketing", tone: "coral" },
  { id: mockIds.spaces.hr, label: "Phòng Nhân sự", tone: "sage" },
];

// ── Fixtures of the analytics read model ────────────────────────────────

/** "Phân hệ ERP" of the ERP items (the board field holds ERP-142 only). */
const ERP_MODULES: Readonly<Record<string, string | null>> = {
  "ERP-142": "Kế toán",
  "ERP-151": "Kế toán",
  "ERP-144": "Kế toán",
  "ERP-140": "Kế toán",
  "ERP-149": "Kế toán",
  "ERP-146": "Kho",
  "ERP-150": "Kho",
  "ERP-153": "Mua hàng",
  "ERP-147": "Mua hàng",
  "ERP-138": null,
  "ERP-152": null,
  "ERP-131": "Kho",
  "ERP-129": "Kế toán",
  "ERP-136": null,
  "ERP-133": "Kho",
  "ERP-135": "Kế toán",
};

/** Finished items per week on the ERP board, oldest first (Monday-based). */
const ERP_WEEKLY_DONE: readonly number[] = [2, 2, 1];

/** The department roll-up the weekly report of the executives shows. */
const WEEK_DEPARTMENTS: readonly {
  label: string;
  tone: ReportTone;
  done: number;
  overdue: number;
}[] = [
  { label: "Kế toán", tone: "gold", done: 4, overdue: 1 },
  { label: "Marketing", tone: "coral", done: 3, overdue: 0 },
  { label: "Nhân sự", tone: "sage", done: 2, overdue: 1 },
  { label: "Công nghệ", tone: "blue", done: 1, overdue: 3 },
  { label: "Ban giám đốc", tone: "crimson", done: 0, overdue: 0 },
];

// ── Time helpers (days are Hanoi days, UTC+7) ───────────────────────────

const DAY_MS = 86_400_000;
const HANOI_MS = 7 * 3_600_000;

/** The Hanoi calendar day of an instant as "YYYY-MM-DD". */
export function hanoiDay(ms: number): string {
  return isoOfMs(ms + HANOI_MS).slice(0, 10);
}

function dayMs(day: string): number {
  return Date.parse(`${day}T00:00:00Z`);
}

function addDays(day: string, days: number): string {
  return isoOfMs(dayMs(day) + days * DAY_MS).slice(0, 10);
}

/** Monday of the week the day falls in. */
function mondayOf(day: string): string {
  const weekday = utcWeekdayOfMs(dayMs(day));
  return addDays(day, -((weekday + 6) % 7));
}

/** A due moment with no time of day (Hanoi midnight) counts to the day's end. */
function dueEndMs(dueAt: string): number {
  const ms = Date.parse(dueAt);
  const local = (ms + HANOI_MS) % DAY_MS;
  return local === 0 ? ms + DAY_MS : ms;
}

// ── Board items ─────────────────────────────────────────────────────────

export interface ReportItem {
  id: string;
  key: string;
  title: string;
  boardId: string;
  boardName: string;
  bucket: ReportBucket;
  statusLabel: string;
  priority: ReportPriority;
  assigneeId: string | null;
  dueAt: string | null;
  completedAt: string | null;
  module: string | null;
}

const BUCKET_OF: Readonly<Record<string, ReportBucket>> = {
  todo: "todo",
  in_progress: "doing",
  review: "review",
  done: "done",
};

const PRIORITY_BY_NAME: Readonly<Record<string, ReportPriority>> = {
  Khẩn: "urgent",
  Cao: "high",
  "Trung bình": "medium",
  Thấp: "low",
};

/** The listed, unarchived items of a board, with Status and Priority named. */
export function boardItems(store: MockStore, boardId: string): ReportItem[] {
  const board = store.getBoard(boardId);
  if (!board) return [];
  const columns = store.getColumns(boardId);
  const statusColumn = columns.find((column) => column.fieldType === "status");
  const priorityColumn = columns.find((column) => column.name === "Ưu tiên");
  const items: ReportItem[] = [];
  for (const list of store.getLists(boardId)) {
    for (const card of store.getCards(list.id)) {
      if (card.parentItemId || card.isArchived) continue;
      const values = store.getFieldValues(card.id);
      const status = statusColumn?.options?.find(
        (option) => option.id === values[statusColumn.id],
      );
      const priority = priorityColumn?.options?.find(
        (option) => option.id === values[priorityColumn.id],
      );
      const bucket =
        BUCKET_OF[status?.category ?? "todo"] ??
        (card.completedAt ? "done" : "todo");
      const key = card.itemKey ?? card.id;
      items.push({
        id: card.id,
        key,
        title: card.title,
        boardId,
        boardName: board.title,
        bucket: card.completedAt ? "done" : bucket,
        statusLabel: status?.name ?? "",
        priority: PRIORITY_BY_NAME[priority?.name ?? ""] ?? "medium",
        assigneeId: card.assigneeUserIds?.[0] ?? null,
        dueAt: card.dueAt ?? null,
        completedAt: card.completedAt ?? null,
        module:
          boardId === mockIds.boards.erp ? (ERP_MODULES[key] ?? null) : null,
      });
    }
  }
  return items;
}

/** Boards a person can count in a report: listed boards that hold items. */
export function reportableBoards(
  store: MockStore,
  workspaceId: string,
): { id: string; name: string; dimensions: GroupBy[] }[] {
  return store
    .getBoards(workspaceId)
    .filter(
      (board) => !board.isArchived && boardItems(store, board.id).length > 0,
    )
    .map((board) => ({
      id: board.id,
      name: board.title,
      dimensions: [
        "assignee",
        "status",
        "priority",
        ...(board.id === mockIds.boards.erp ? (["module"] as const) : []),
        "week",
      ] as GroupBy[],
    }));
}

const isOpen = (item: ReportItem) => item.bucket !== "done";
const isOverdue = (item: ReportItem, nowMs: number) =>
  isOpen(item) && item.dueAt !== null && dueEndMs(item.dueAt) < nowMs;

// ── Aggregates ──────────────────────────────────────────────────────────

export interface GroupRow {
  key: string;
  label: string;
  count: number;
  person: ReportPerson | null;
  /** The row of the items nobody owns / no value ("Chưa giao", "Chưa đặt"). */
  none: boolean;
  priority: ReportPriority | null;
  bucket: ReportBucket | null;
}

const PRIORITY_ORDER: readonly ReportPriority[] = [
  "urgent",
  "high",
  "medium",
  "low",
];
const BUCKET_ORDER: readonly ReportBucket[] = [
  "todo",
  "doing",
  "review",
  "done",
];

/** The weeks of the velocity chart: three past, this week and two to come. */
function chartWeeks(nowMs: number): { from: string; to: string }[] {
  const monday = mondayOf(hanoiDay(nowMs));
  return Array.from({ length: 6 }, (_, index) => {
    const from = addDays(monday, (index - 3) * 7);
    return { from, to: addDays(from, 6) };
  });
}

function weeklyDone(
  items: readonly ReportItem[],
  boardId: string | null,
  nowMs: number,
): number[] {
  const weeks = chartWeeks(nowMs).slice(0, 4);
  return weeks.map((week, index) => {
    if (boardId === mockIds.boards.erp && index < 3)
      return ERP_WEEKLY_DONE[index] ?? 0;
    return items.filter((item) => {
      if (!item.completedAt) return false;
      const day = hanoiDay(Date.parse(item.completedAt));
      return day >= week.from && day <= week.to;
    }).length;
  });
}

function applyFilters(
  items: readonly ReportItem[],
  filters: readonly FilterId[],
  nowMs: number,
): ReportItem[] {
  return items.filter((item) =>
    filters.every((filter) => {
      switch (filter) {
        case "not-done":
          return isOpen(item);
        case "overdue":
          return isOverdue(item, nowMs);
        case "assigned":
          return item.assigneeId !== null;
        case "unassigned":
          return item.assigneeId === null;
        case "high-priority":
          return item.priority === "urgent" || item.priority === "high";
      }
    }),
  );
}

export function groupItems(
  store: MockStore,
  items: readonly ReportItem[],
  groupBy: GroupBy,
  boardId: string | null,
  nowMs: number,
): GroupRow[] {
  const row = (
    key: string,
    label: string,
    count: number,
    extra: Partial<GroupRow> = {},
  ): GroupRow => ({
    key,
    label,
    count,
    person: null,
    none: false,
    priority: null,
    bucket: null,
    ...extra,
  });
  if (groupBy === "status")
    return BUCKET_ORDER.map((bucket) =>
      row(
        bucket,
        bucket,
        items.filter((item) => item.bucket === bucket).length,
        { bucket },
      ),
    );
  if (groupBy === "priority")
    return PRIORITY_ORDER.map((priority) =>
      row(
        priority,
        priority,
        items.filter((item) => item.priority === priority).length,
        { priority },
      ),
    );
  if (groupBy === "module") {
    const names = [
      ...new Set(
        items.map((item) => item.module).filter((m): m is string => m !== null),
      ),
    ];
    const rows = names
      .map((name) =>
        row(name, name, items.filter((item) => item.module === name).length),
      )
      .sort((a, b) => b.count - a.count);
    const none = items.filter((item) => item.module === null).length;
    return none > 0 ? [...rows, row("none", "", none, { none: true })] : rows;
  }
  if (groupBy === "week")
    return weeklyDone(items, boardId, nowMs).map((count, index) => {
      const week = chartWeeks(nowMs)[index]!;
      return row(week.from, week.from, count);
    });
  // assignee
  const ids = [
    ...new Set(
      items.map((item) => item.assigneeId).filter((i): i is string => !!i),
    ),
  ];
  const rows = ids
    .map((id) => {
      const mine = items.filter((item) => item.assigneeId === id);
      return {
        id,
        count: mine.length,
        overdue: mine.filter((item) => isOverdue(item, nowMs)).length,
      };
    })
    .sort(
      (a, b) =>
        b.count - a.count ||
        b.overdue - a.overdue ||
        orderOf(a.id) - orderOf(b.id),
    )
    .map((entry) =>
      row(entry.id, personOf(store, entry.id)?.name ?? entry.id, entry.count, {
        person: personOf(store, entry.id),
      }),
    );
  const none = items.filter((item) => item.assigneeId === null).length;
  return none > 0 ? [...rows, row("none", "", none, { none: true })] : rows;
}

function orderOf(userId: string): number {
  const index = PERSON_ORDER.indexOf(userId);
  return index < 0 ? PERSON_ORDER.length : index;
}

// ── Widget data ─────────────────────────────────────────────────────────

export interface WidgetContext {
  store: MockStore;
  report: StoredReport;
  nowMs: number;
  dataAsOf: string;
  /** Report filter: only this person's items; null = everyone. */
  assigneeId: string | null;
  viewerId: string;
}

function boardOf(spec: WidgetSpec, ctx: WidgetContext): string | null {
  return spec.boardId ?? ctx.report.sourceBoardId;
}

function scoped(
  ctx: WidgetContext,
  boardId: string | null,
  items: ReportItem[],
): ReportItem[] {
  void boardId;
  return ctx.assigneeId
    ? items.filter((item) => item.assigneeId === ctx.assigneeId)
    : items;
}

export interface DueItem {
  id: string;
  key: string;
  title: string;
  boardId: string;
  bucket: ReportBucket;
  statusLabel: string;
  assignee: ReportPerson | null;
  day: string;
  priority: ReportPriority;
  overdueDays: number;
  module: string | null;
  awaitingViewer: boolean;
}

/** An item the signed-in person is asked to approve (the mockup's ERP-138). */
const AWAITING_APPROVAL_OF_OWNER = new Set(["ERP-138"]);

function toDueItem(ctx: WidgetContext, item: ReportItem): DueItem {
  const day = item.dueAt ? hanoiDay(Date.parse(item.dueAt)) : "";
  const today = hanoiDay(ctx.nowMs);
  return {
    id: item.id,
    key: item.key,
    title: item.title,
    boardId: item.boardId,
    bucket: item.bucket,
    statusLabel: item.statusLabel,
    assignee: item.assigneeId ? personOf(ctx.store, item.assigneeId) : null,
    day,
    priority: item.priority,
    overdueDays:
      item.dueAt && isOverdue(item, ctx.nowMs)
        ? Math.max(1, Math.round((dayMs(today) - dayMs(day)) / DAY_MS))
        : 0,
    module: item.module,
    awaitingViewer:
      ctx.viewerId === mockIds.users.owner &&
      AWAITING_APPROVAL_OF_OWNER.has(item.key) &&
      item.bucket === "review",
  };
}

function dueWindow(ctx: WidgetContext): { from: string; to: string } {
  const today = hanoiDay(ctx.nowMs);
  return { from: addDays(today, 1), to: addDays(today, 7) };
}

function dueSoon(ctx: WidgetContext, items: readonly ReportItem[]) {
  const window = dueWindow(ctx);
  return items
    .filter((item) => {
      if (!isOpen(item) || !item.dueAt) return false;
      const day = hanoiDay(Date.parse(item.dueAt));
      return day >= window.from && day <= window.to;
    })
    .sort((a, b) => Date.parse(a.dueAt!) - Date.parse(b.dueAt!));
}

export interface SummaryDto {
  done: number;
  total: number;
  overdue: number;
  dueSoon: number;
  launchDate: string | null;
  daysToLaunch: number | null;
}

export function summaryOf(ctx: WidgetContext, boardId: string): SummaryDto {
  const items = scoped(ctx, boardId, boardItems(ctx.store, boardId));
  const isErp = boardId === mockIds.boards.erp;
  const today = hanoiDay(ctx.nowMs);
  return {
    done: items.filter((item) => !isOpen(item)).length,
    total: items.length,
    overdue: items.filter((item) => isOverdue(item, ctx.nowMs)).length,
    dueSoon: dueSoon(ctx, items).length,
    launchDate: isErp ? ERP_LAUNCH_DAY : null,
    daysToLaunch: isErp
      ? Math.round((dayMs(ERP_LAUNCH_DAY) - dayMs(today)) / DAY_MS)
      : null,
  };
}

export function widgetData(spec: WidgetSpec, ctx: WidgetContext) {
  const boardId = boardOf(spec, ctx);
  const base = {
    id: spec.id,
    spec,
    size: spec.size,
    title: spec.title ?? null,
    titleKey: spec.titleKey ?? null,
    staleAsOf: null as string | null,
  };
  const items = boardId
    ? scoped(ctx, boardId, boardItems(ctx.store, boardId))
    : [];
  const board = boardId ? ctx.store.getBoard(boardId) : undefined;
  const boardName = board?.title ?? null;

  switch (spec.kind) {
    case "status": {
      const counts = {
        todo: 0,
        doing: 0,
        review: 0,
        done: 0,
      } as Record<ReportBucket, number>;
      for (const item of items) counts[item.bucket] += 1;
      return {
        ...base,
        kind: "status" as const,
        titleKey: base.titleKey ?? "status",
        total: items.length,
        counts,
      };
    }
    case "velocity": {
      const weeks = chartWeeks(ctx.nowMs);
      const done = weeklyDone(items, boardId, ctx.nowMs);
      const currentIndex = 3;
      const open = items.filter(isOpen).length;
      const isErp = boardId === mockIds.boards.erp;
      const launch = isErp ? ERP_LAUNCH_DAY : null;
      const past = done.slice(0, 3);
      const average = past.reduce((a, b) => a + b, 0) / 3;
      const weeksLeft = launch
        ? Math.max(
            1,
            Math.round(
              (dayMs(mondayOf(launch)) - dayMs(weeks[currentIndex]!.from)) /
                (7 * DAY_MS),
            ) + 1,
          )
        : null;
      const pace = weeksLeft ? open / weeksLeft : null;
      const verdict =
        pace === null ? "none" : pace > average ? "late" : "ontrack";
      return {
        ...base,
        kind: "velocity" as const,
        titleKey: base.titleKey ?? "velocity",
        weeks: weeks.map((week, index) => ({
          from: week.from,
          to: week.to,
          count: index <= currentIndex ? (done[index] ?? 0) : null,
          state:
            index < currentIndex
              ? ("past" as const)
              : index === currentIndex
                ? ("current" as const)
                : ("future" as const),
        })),
        past,
        average,
        pace,
        openCount: open,
        weeksLeft,
        launchDate: launch,
        yMax: Math.max(5, Math.ceil(Math.max(...done, pace ?? 0))),
        verdict,
      };
    }
    case "module":
    case "priority": {
      const group = spec.kind === "module" ? "module" : "priority";
      const scope = group === "module" ? items.filter(isOpen) : items;
      const rows = groupItems(ctx.store, scope, group, boardId, ctx.nowMs);
      return {
        ...base,
        kind: "bars" as const,
        variant: group,
        titleKey: base.titleKey ?? group,
        subtitle: group === "module" ? ("open" as const) : ("all" as const),
        total: scope.length,
        max: Math.max(1, ...rows.map((row) => row.count)),
        rows,
      };
    }
    case "due": {
      const window = dueWindow(ctx);
      const list = dueSoon(ctx, items);
      return {
        ...base,
        kind: "due" as const,
        titleKey: base.titleKey ?? "due",
        from: window.from,
        to: window.to,
        items: list.map((item) => toDueItem(ctx, item)),
      };
    }
    case "workload":
    case "progress":
    case "number":
    case "columns":
    case "line":
    case "donut":
    case "table":
      return customData(spec, ctx, base, items, boardId, boardName);
    case "dept-done":
    case "dept-overdue": {
      const done = spec.kind === "dept-done";
      const stale = !done && !ctx.report.recomputed;
      const rows = WEEK_DEPARTMENTS.map((dept) => ({
        key: dept.label,
        label: dept.label,
        count: done ? dept.done : dept.overdue,
        person: null,
        none: false,
        priority: null,
        bucket: null,
        tone: dept.tone,
      })).sort((a, b) => b.count - a.count);
      return {
        ...base,
        kind: "bars" as const,
        variant: "department" as const,
        titleKey: spec.kind === "dept-done" ? "deptDone" : "deptOverdue",
        subtitle: done ? ("week-done" as const) : ("week-overdue" as const),
        total: rows.reduce((a, b) => a + b.count, 0),
        max: Math.max(1, ...rows.map((row) => row.count)),
        rows,
        tone: done ? "accent" : "danger",
        weekFrom: "2026-10-05",
        weekTo: "2026-10-11",
        staleAsOf: stale ? "2026-10-12T03:20:00.000Z" : null,
      };
    }
    case "note":
      return {
        ...base,
        kind: "note" as const,
        titleKey: spec.title ? null : "note",
        author: spec.authorId ? personOf(ctx.store, spec.authorId) : null,
        at: spec.writtenOn ?? null,
        paragraphs: (spec.text ?? "")
          .split("\n")
          .map((line) => line.trim())
          .filter(Boolean),
      };
    case "erp-progress": {
      const erpItems = scoped(
        ctx,
        mockIds.boards.erp,
        boardItems(ctx.store, mockIds.boards.erp),
      );
      const today = hanoiDay(ctx.nowMs);
      return {
        ...base,
        kind: "progress" as const,
        titleKey: "erpProgress",
        done: erpItems.filter((item) => !isOpen(item)).length,
        total: erpItems.length,
        boardId: mockIds.boards.erp,
        boardName: ctx.store.getBoard(mockIds.boards.erp)?.title ?? null,
        launchDate: ERP_LAUNCH_DAY,
        daysLeft: Math.round((dayMs(ERP_LAUNCH_DAY) - dayMs(today)) / DAY_MS),
        overdue: erpItems.filter((item) => isOverdue(item, ctx.nowMs)).length,
        link: true,
      };
    }
  }
}

function customData(
  spec: WidgetSpec,
  ctx: WidgetContext,
  base: {
    id: string;
    spec: WidgetSpec;
    size: WidgetSize;
    title: string | null;
    titleKey: string | null;
    staleAsOf: string | null;
  },
  items: ReportItem[],
  boardId: string | null,
  boardName: string | null,
) {
  const filters = spec.filters ?? [];
  const metric: Metric = spec.metric ?? "count";
  const kept = applyFilters(items, filters, ctx.nowMs).filter((item) =>
    metric === "overdue" ? isOverdue(item, ctx.nowMs) : true,
  );
  const common = { ...base, boardId, boardName, metric, total: kept.length };
  if (spec.kind === "workload") {
    const rows = groupItems(ctx.store, kept, "assignee", boardId, ctx.nowMs);
    return {
      ...common,
      kind: "workload" as const,
      titleKey: base.titleKey ?? (spec.title ? null : "workload"),
      rows: rows.map((row) => ({
        key: row.key,
        person: row.person,
        open: row.count,
        overdue: kept.filter(
          (item) =>
            (item.assigneeId ?? "none") === row.key &&
            isOverdue(item, ctx.nowMs),
        ).length,
      })),
      max: Math.max(1, ...rows.map((row) => row.count)),
    };
  }
  if (spec.kind === "progress") {
    return {
      ...common,
      kind: "progress" as const,
      titleKey: base.titleKey ?? null,
      done: items.filter((item) => !isOpen(item)).length,
      total: items.length,
      launchDate: null,
      daysLeft: null,
      overdue: items.filter((item) => isOverdue(item, ctx.nowMs)).length,
      link: false,
    };
  }
  if (spec.kind === "table") {
    return {
      ...common,
      kind: "table" as const,
      titleKey: base.titleKey ?? null,
      items: kept
        .sort(
          (a, b) =>
            (a.dueAt ? Date.parse(a.dueAt) : Infinity) -
            (b.dueAt ? Date.parse(b.dueAt) : Infinity),
        )
        .slice(0, 12)
        .map((item) => toDueItem(ctx, item)),
    };
  }
  if (spec.kind === "number") {
    return {
      ...common,
      kind: "number" as const,
      titleKey: base.titleKey ?? null,
      value: kept.length,
      of: items.length,
    };
  }
  const groupBy: GroupBy = spec.groupBy ?? "assignee";
  const rows = groupItems(ctx.store, kept, groupBy, boardId, ctx.nowMs);
  const kind =
    spec.kind === "line" ? "line" : spec.kind === "donut" ? "donut" : "columns";
  return {
    ...common,
    kind: kind as "line" | "donut" | "columns",
    titleKey: base.titleKey ?? null,
    groupBy,
    rows,
    max: Math.max(1, ...rows.map((row) => row.count)),
  };
}

// ── Drill-down ──────────────────────────────────────────────────────────

export function drillDown(
  ctx: WidgetContext,
  boardId: string,
  assigneeKey: string,
): { person: ReportPerson | null; items: DueItem[] } | null {
  const items = boardItems(ctx.store, boardId).filter(
    (item) =>
      isOpen(item) &&
      (assigneeKey === "none"
        ? item.assigneeId === null
        : item.assigneeId === assigneeKey),
  );
  if (items.length === 0 && assigneeKey !== "none") {
    if (!personOf(ctx.store, assigneeKey)) return null;
  }
  return {
    person: assigneeKey === "none" ? null : personOf(ctx.store, assigneeKey),
    items: items
      .sort(
        (a, b) =>
          (a.dueAt ? Date.parse(a.dueAt) : Infinity) -
          (b.dueAt ? Date.parse(b.dueAt) : Infinity),
      )
      .map((item) => toDueItem(ctx, item)),
  };
}

// ── Layout presets ──────────────────────────────────────────────────────

export type LayoutId = "project" | "team" | "deadline";

export function presetWidgets(
  layout: LayoutId,
  boardId: string,
  nextId: () => string,
): WidgetSpec[] {
  const w = (spec: Omit<WidgetSpec, "id" | "boardId">): WidgetSpec => ({
    id: nextId(),
    boardId,
    ...spec,
  });
  if (layout === "project")
    return [
      w({ kind: "status", size: "wide" }),
      w({ kind: "velocity", size: "wide" }),
      w({ kind: "due", size: "side" }),
    ];
  if (layout === "team")
    return [
      w({ kind: "workload", size: "side", filters: ["not-done"] }),
      w({
        kind: "columns",
        size: "wide",
        groupBy: "assignee",
        metric: "overdue",
        titleKey: "overdueByPerson",
      }),
      w({
        kind: "number",
        size: "half",
        filters: ["not-done", "unassigned"],
        titleKey: "unassigned",
      }),
    ];
  return [
    w({
      kind: "number",
      size: "half",
      metric: "overdue",
      titleKey: "overdueCount",
    }),
    w({ kind: "due", size: "side" }),
    w({
      kind: "table",
      size: "wide",
      filters: ["not-done"],
      titleKey: "byDeadline",
    }),
  ];
}

// ── Exports ─────────────────────────────────────────────────────────────

export interface ExportJob {
  id: string;
  reportId: string;
  format: "pdf" | "csv";
  startedAt: string;
  /** How many times the job has been read; it finishes on the second. */
  checks: number;
  status: "running" | "done";
  filename: string;
  content: string;
}

export function csvOf(
  reportName: string,
  widgets: readonly {
    title: string;
    rows: readonly (readonly (string | number)[])[];
  }[],
): string {
  const escape = (value: string | number) =>
    /[",\n]/.test(String(value))
      ? `"${String(value).replace(/"/g, '""')}"`
      : String(value);
  const lines = [`# ${reportName}`];
  for (const widget of widgets) {
    lines.push("", `# ${widget.title}`);
    for (const row of widget.rows) lines.push(row.map(escape).join(","));
  }
  return lines.join("\n");
}

// ── The store ───────────────────────────────────────────────────────────

export class MockReports {
  private reports = new Map<string, StoredReport>();
  private jobs = new Map<string, ExportJob>();
  private sequence = 0;
  private seeded = false;

  reset(): void {
    this.reports.clear();
    this.jobs.clear();
    this.sequence = 0;
    this.seeded = false;
  }

  nextId(prefix: string): string {
    this.sequence += 1;
    return `${prefix}-${this.sequence}`;
  }

  private seed(): void {
    if (this.seeded) return;
    this.seeded = true;
    const id = (suffix: string) => `mock-widget-${suffix}`;
    const erpBoard = mockIds.boards.erp;
    const t = (hour: number, minute: number) =>
      isoOfMs(Date.parse(hanoiInstant("12/10", hour)) + minute * 60_000);
    this.reports.set(REPORT_IDS.erp, {
      id: REPORT_IDS.erp,
      name: "Tổng quan triển khai ERP Q4",
      scopeId: SCOPES.tech.id,
      scopeLabel: SCOPES.tech.label,
      scopeTone: SCOPES.tech.tone,
      ownerId: ERP_CREATOR_ID,
      sharedWith: ["Phòng Công nghệ", "Ban giám đốc"],
      includesViewer: false,
      editorIds: [mockIds.users.owner, ERP_CREATOR_ID],
      createdAt: "2026-10-06T04:00:00.000Z",
      updatedAt: t(10, 38),
      dataAsOf: t(10, 37),
      sourceBoardId: erpBoard,
      multiSource: false,
      excludedBoardCount: 0,
      widgetCountShown: true,
      recomputed: true,
      widgets: [
        { id: id("status"), kind: "status", size: "wide" },
        { id: id("velocity"), kind: "velocity", size: "wide" },
        { id: id("module"), kind: "module", size: "half" },
        { id: id("priority"), kind: "priority", size: "half" },
        { id: id("due"), kind: "due", size: "side" },
        {
          id: id("workload"),
          kind: "workload",
          size: "side",
          filters: ["not-done"],
        },
      ].map((spec) => ({ ...spec, boardId: erpBoard })) as WidgetSpec[],
    });
    this.reports.set(REPORT_IDS.close, {
      id: REPORT_IDS.close,
      name: "Đóng sổ tháng 10",
      scopeId: SCOPES.accounting.id,
      scopeLabel: SCOPES.accounting.label,
      scopeTone: SCOPES.accounting.tone,
      ownerId: mockIds.users.commenter,
      sharedWith: ["Phòng Kế toán"],
      includesViewer: true,
      editorIds: [mockIds.users.commenter],
      createdAt: "2026-10-05T04:00:00.000Z",
      updatedAt: t(10, 35),
      dataAsOf: t(10, 34),
      sourceBoardId: mockIds.boards.accounting,
      multiSource: false,
      excludedBoardCount: 0,
      widgetCountShown: false,
      recomputed: true,
      widgets: [
        { id: id("close-status"), kind: "status", size: "wide" },
        { id: id("close-due"), kind: "due", size: "side" },
        {
          id: id("close-workload"),
          kind: "workload",
          size: "side",
          filters: ["not-done"],
        },
      ].map((spec) => ({
        ...spec,
        boardId: mockIds.boards.accounting,
      })) as WidgetSpec[],
    });
    this.reports.set(REPORT_IDS.weekly, {
      id: REPORT_IDS.weekly,
      name: "Báo cáo tuần Ban giám đốc",
      scopeId: SCOPES.executive.id,
      scopeLabel: SCOPES.executive.label,
      scopeTone: SCOPES.executive.tone,
      ownerId: EXEC_AUTHOR.id,
      sharedWith: ["Ban giám đốc"],
      includesViewer: true,
      editorIds: [EXEC_AUTHOR.id],
      createdAt: "2026-10-04T04:00:00.000Z",
      updatedAt: t(10, 36),
      dataAsOf: t(10, 35),
      sourceBoardId: null,
      multiSource: true,
      excludedBoardCount: 1,
      widgetCountShown: false,
      recomputed: false,
      widgets: [
        { id: id("w-done"), kind: "dept-done", size: "wide" },
        { id: id("w-over"), kind: "dept-overdue", size: "wide" },
        {
          id: id("w-note"),
          kind: "note",
          size: "side",
          title: "Ghi chú tuần",
          authorId: EXEC_AUTHOR.id,
          writtenOn: "2026-10-09",
          text: "Trọng tâm tuần 12–18/10: giữ mốc go-live ERP 01/11 và đóng sổ tháng 10.\nCác phòng cập nhật trạng thái việc trước 17:00 thứ Sáu để số liệu tuần sau đầy đủ.",
        },
        {
          id: id("w-erp"),
          kind: "erp-progress",
          size: "side",
          boardId: erpBoard,
        },
      ],
    });
  }

  list(): StoredReport[] {
    this.seed();
    return [...this.reports.values()].sort(
      (a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt),
    );
  }

  get(id: string): StoredReport | undefined {
    this.seed();
    return this.reports.get(id);
  }

  put(report: StoredReport): void {
    this.reports.set(report.id, report);
  }

  remove(id: string): boolean {
    return this.reports.delete(id);
  }

  addJob(job: ExportJob): void {
    this.jobs.set(job.id, job);
  }

  job(id: string): ExportJob | undefined {
    return this.jobs.get(id);
  }
}
