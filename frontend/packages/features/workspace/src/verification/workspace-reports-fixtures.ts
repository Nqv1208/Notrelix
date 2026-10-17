import { createFormatters } from "@notrelix/i18n";
import type { ReportDashboardSurfaceProps } from "../web/reports/report-dashboard-surface";
import type { ReportListSurfaceProps } from "../web/reports/report-list-surface";
import type { ReportLinks } from "../web/reports/report-widgets";
import {
  type ReportDto,
  type ReportDueItem,
  type ReportGroupRow,
  type ReportListItemDto,
  type ReportPersonDto,
  type ReportWidgetDto,
  type ReportWidgetOptionsDto,
  type ReportWidgetSpec,
} from "../web/reports/reports-models";
import { reportsCopy } from "../web/reports/reports.copy";

/**
 * Mockup world of the Reports pages (Final-Reports), for stories and tests
 * only: Minh An Group on Monday 12/10/2026 10:40 (GMT+7), the ERP board
 * "Triển khai ERP Q4" and the weekly report of the executives. Deterministic:
 * no clock, no random.
 */
const noop = () => undefined;
export const REPORTS_FIXTURE_COPY = reportsCopy("vi");
export const REPORTS_FIXTURE_FORMATTERS = createFormatters("vi");

const TIME_ZONE = "Asia/Ho_Chi_Minh";
const ERP_BOARD = { id: "board-erp", name: "Triển khai ERP Q4" } as const;

export const PEOPLE = {
  quan: { id: "user-quan", name: "Trần Minh Quân", tone: "blue" },
  ngocAnh: { id: "user-ngoc-anh", name: "Phan Ngọc Anh", tone: "violet" },
  thanh: { id: "user-thanh", name: "Vũ Thanh", tone: "sage" },
  phuc: { id: "user-phuc", name: "Lê Hoàng Phúc", tone: "coral" },
  tam: { id: "user-tam", name: "Bùi Thanh Tâm", tone: "sage" },
  linh: { id: "user-linh", name: "Đỗ Khánh Linh", tone: "gold" },
  ha: { id: "user-ha", name: "Nguyễn Thu Hà", tone: "sage" },
  hung: { id: "user-hung", name: "Trịnh Quốc Hưng", tone: "crimson" },
} as const satisfies Record<string, ReportPersonDto>;

const spec = (
  id: string,
  kind: string,
  size: "wide" | "half" | "side",
  extra: Partial<ReportWidgetSpec> = {},
): ReportWidgetSpec => ({ id, kind, size, boardId: ERP_BOARD.id, ...extra });

const row = (
  key: string,
  label: string,
  count: number,
  extra: Partial<ReportGroupRow> = {},
): ReportGroupRow => ({
  key,
  label,
  count,
  person: null,
  none: false,
  priority: null,
  bucket: null,
  ...extra,
});

const base = (id: string, kind: string, size: "wide" | "half" | "side") => ({
  id,
  size,
  title: null,
  titleKey: null as string | null,
  staleAsOf: null,
  spec: spec(id, kind, size),
});

const due = (
  key: string,
  title: string,
  bucket: ReportDueItem["bucket"],
  statusLabel: string,
  assignee: ReportPersonDto | null,
  day: string,
  extra: Partial<ReportDueItem> = {},
): ReportDueItem => ({
  id: `item-${key}`,
  key,
  title,
  boardId: ERP_BOARD.id,
  bucket,
  statusLabel,
  assignee,
  day,
  priority: "medium",
  overdueDays: 0,
  module: null,
  awaitingViewer: false,
  ...extra,
});

export const DUE_SOON: readonly ReportDueItem[] = [
  due(
    "ERP-138",
    "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    "review",
    "Chờ duyệt",
    PEOPLE.quan,
    "2026-10-14",
    { priority: "high", awaitingViewer: true },
  ),
  due(
    "ERP-140",
    "Mẫu báo cáo doanh thu theo chi nhánh",
    "review",
    "Chờ duyệt",
    PEOPLE.ngocAnh,
    "2026-10-15",
  ),
  due(
    "ERP-146",
    "Đồng bộ tồn kho giữa kho Bình Dương và kho Long An",
    "doing",
    "Đang làm",
    PEOPLE.thanh,
    "2026-10-16",
  ),
  due(
    "ERP-151",
    "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
    "todo",
    "Cần làm",
    PEOPLE.ngocAnh,
    "2026-10-18",
    { priority: "high" },
  ),
];

export const QUAN_ITEMS: readonly ReportDueItem[] = [
  due(
    "ERP-142",
    "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    "doing",
    "Đang làm",
    PEOPLE.quan,
    "2026-10-10",
    { priority: "urgent", overdueDays: 2, module: "Kế toán" },
  ),
  DUE_SOON[0]!,
];

const WORKLOAD = [
  [PEOPLE.quan, 2, 1],
  [PEOPLE.ngocAnh, 2, 0],
  [PEOPLE.thanh, 2, 0],
  [PEOPLE.phuc, 1, 0],
  [PEOPLE.tam, 1, 0],
  [PEOPLE.linh, 1, 0],
  [PEOPLE.ha, 1, 0],
] as const;

export function erpWidgets(): ReportWidgetDto[] {
  return [
    {
      ...base("w-status", "status", "wide"),
      kind: "status",
      titleKey: "status",
      total: 16,
      counts: { todo: 6, doing: 3, review: 2, done: 5 },
    },
    {
      ...base("w-velocity", "velocity", "wide"),
      kind: "velocity",
      titleKey: "velocity",
      weeks: [
        { from: "2026-09-21", to: "2026-09-27", count: 2, state: "past" },
        { from: "2026-09-28", to: "2026-10-04", count: 2, state: "past" },
        { from: "2026-10-05", to: "2026-10-11", count: 1, state: "past" },
        { from: "2026-10-12", to: "2026-10-18", count: 0, state: "current" },
        { from: "2026-10-19", to: "2026-10-25", count: null, state: "future" },
        { from: "2026-10-26", to: "2026-11-01", count: null, state: "future" },
      ],
      past: [2, 2, 1],
      average: 5 / 3,
      pace: 11 / 3,
      openCount: 11,
      weeksLeft: 3,
      launchDate: "2026-11-01",
      yMax: 5,
      verdict: "late",
    },
    {
      ...base("w-module", "module", "half"),
      kind: "bars",
      variant: "module",
      titleKey: "module",
      subtitle: "open",
      total: 11,
      max: 5,
      rows: [
        row("Kế toán", "Kế toán", 5),
        row("Kho", "Kho", 2),
        row("Mua hàng", "Mua hàng", 2),
        row("none", "", 2, { none: true }),
      ],
    },
    {
      ...base("w-priority", "priority", "half"),
      kind: "bars",
      variant: "priority",
      titleKey: "priority",
      subtitle: "all",
      total: 16,
      max: 7,
      rows: [
        row("urgent", "urgent", 1, { priority: "urgent" }),
        row("high", "high", 5, { priority: "high" }),
        row("medium", "medium", 7, { priority: "medium" }),
        row("low", "low", 3, { priority: "low" }),
      ],
    },
    {
      ...base("w-due", "due", "side"),
      kind: "due",
      titleKey: "due",
      from: "2026-10-13",
      to: "2026-10-19",
      items: DUE_SOON,
    },
    {
      ...base("w-workload", "workload", "side"),
      kind: "workload",
      titleKey: "workload",
      total: 11,
      max: 2,
      rows: [
        ...WORKLOAD.map(([person, open, overdue]) => ({
          key: person.id,
          person,
          open,
          overdue,
        })),
        { key: "none", person: null, open: 1, overdue: 0 },
      ],
    },
  ];
}

const erpBase: Omit<ReportDto, "widgets"> = {
  id: "report-erp",
  name: "Tổng quan triển khai ERP Q4",
  scope: { id: "space-tech", label: "Phòng Công nghệ", tone: "blue" },
  owner: PEOPLE.phuc,
  sharedWith: ["Phòng Công nghệ", "Ban giám đốc"],
  includesViewer: false,
  access: "edit",
  createdAt: "2026-10-06T04:00:00.000Z",
  updatedAt: "2026-10-12T03:38:00.000Z",
  dataAsOf: "2026-10-12T03:37:00.000Z",
  timeZone: TIME_ZONE,
  excludedBoardCount: 0,
  sourceBoard: ERP_BOARD,
  filters: {
    board: {
      selectedId: ERP_BOARD.id,
      options: [{ id: ERP_BOARD.id, name: ERP_BOARD.name }],
    },
    time: { kind: "until", day: "2026-11-01" },
    assignee: {
      options: [PEOPLE.quan, PEOPLE.ngocAnh, PEOPLE.thanh, PEOPLE.phuc].map(
        (person) => ({ id: person.id, name: person.name }),
      ),
    },
    sources: null,
  },
  summary: {
    done: 5,
    total: 16,
    overdue: 1,
    dueSoon: 4,
    launchDate: "2026-11-01",
    daysToLaunch: 20,
  },
};

export function erpReport(patch: Partial<ReportDto> = {}): ReportDto {
  return { ...erpBase, widgets: erpWidgets(), ...patch };
}

/** The same report read by somebody who may not edit it. */
export function viewerReport(): ReportDto {
  return erpReport({ access: "view" });
}

/** The ERP board with nothing on it: every widget draws its zero. */
export function zeroReport(): ReportDto {
  const zero = (widget: ReportWidgetDto): ReportWidgetDto => {
    switch (widget.kind) {
      case "status":
        return {
          ...widget,
          total: 0,
          counts: { todo: 0, doing: 0, review: 0, done: 0 },
        };
      case "velocity":
        return {
          ...widget,
          weeks: widget.weeks.map((week) => ({
            ...week,
            count: week.count === null ? null : 0,
          })),
          past: [0, 0, 0],
          average: 0,
          pace: 0,
          openCount: 0,
          verdict: "ontrack",
        };
      case "bars":
        return {
          ...widget,
          total: 0,
          max: 1,
          rows: widget.rows.map((r) => ({ ...r, count: 0 })),
        };
      case "due":
        return { ...widget, items: [] };
      case "workload":
        return { ...widget, total: 0, max: 1, rows: [] };
      default:
        return widget;
    }
  };
  return erpReport({
    widgets: erpWidgets().map(zero),
    summary: {
      done: 0,
      total: 0,
      overdue: 0,
      dueSoon: 0,
      launchDate: "2026-11-01",
      daysToLaunch: 20,
    },
  });
}

const DEPARTMENTS = [
  ["Kế toán", "gold", 4, 1],
  ["Marketing", "coral", 3, 0],
  ["Nhân sự", "sage", 2, 1],
  ["Công nghệ", "blue", 1, 3],
  ["Ban giám đốc", "crimson", 0, 0],
] as const;

export function weeklyReport(
  options: { recomputed?: boolean } = {},
): ReportDto {
  const rows = (pick: 2 | 3) =>
    DEPARTMENTS.map(([label, tone, done, overdue]) => ({
      ...row(label, label, pick === 2 ? done : overdue),
      tone,
    })).sort((a, b) => b.count - a.count);
  return {
    ...erpBase,
    id: "report-weekly",
    name: "Báo cáo tuần Ban giám đốc",
    scope: { id: "space-exec", label: "Ban giám đốc", tone: "crimson" },
    owner: PEOPLE.hung,
    sharedWith: ["Ban giám đốc"],
    includesViewer: true,
    access: "view",
    updatedAt: "2026-10-12T03:36:00.000Z",
    dataAsOf: "2026-10-12T03:35:00.000Z",
    excludedBoardCount: 1,
    sourceBoard: null,
    filters: {
      board: null,
      time: { kind: "week", from: "2026-10-05", to: "2026-10-11" },
      assignee: null,
      sources: {
        departments: DEPARTMENTS.map(([label]) => label),
      },
    },
    summary: null,
    widgets: [
      {
        ...base("p-done", "dept-done", "wide"),
        kind: "bars",
        variant: "department",
        titleKey: "deptDone",
        subtitle: "week-done",
        total: 10,
        max: 4,
        rows: rows(2),
        tone: "accent",
        weekFrom: "2026-10-05",
        weekTo: "2026-10-11",
      },
      {
        ...base("p-over", "dept-overdue", "wide"),
        kind: "bars",
        variant: "department",
        titleKey: "deptOverdue",
        subtitle: "week-overdue",
        total: 5,
        max: 3,
        rows: rows(3),
        tone: "danger",
        weekFrom: "2026-10-05",
        weekTo: "2026-10-11",
        staleAsOf: options.recomputed ? null : "2026-10-12T03:20:00.000Z",
      },
      {
        ...base("p-note", "note", "side"),
        kind: "note",
        title: "Ghi chú tuần",
        author: PEOPLE.hung,
        at: "2026-10-09",
        paragraphs: [
          "Trọng tâm tuần 12–18/10: giữ mốc go-live ERP 01/11 và đóng sổ tháng 10.",
          "Các phòng cập nhật trạng thái việc trước 17:00 thứ Sáu để số liệu tuần sau đầy đủ.",
        ],
      },
      {
        ...base("p-erp", "erp-progress", "side"),
        kind: "progress",
        titleKey: "erpProgress",
        done: 5,
        total: 16,
        boardId: ERP_BOARD.id,
        boardName: ERP_BOARD.name,
        launchDate: "2026-11-01",
        daysLeft: 20,
        overdue: 1,
        link: true,
      },
    ],
  };
}

/** A report nobody has put a widget on yet. */
export function emptyReport(): ReportDto {
  return {
    ...erpBase,
    id: "report-new",
    name: null,
    owner: PEOPLE.ha,
    sharedWith: [],
    includesViewer: false,
    createdAt: "2026-10-12T03:40:00.000Z",
    updatedAt: "2026-10-12T03:40:00.000Z",
    dataAsOf: "2026-10-12T03:40:00.000Z",
    sourceBoard: null,
    filters: {
      board: null,
      time: null,
      assignee: null,
      sources: null,
    },
    summary: null,
    widgets: [],
  };
}

export const REPORT_LIST: readonly ReportListItemDto[] = [
  {
    id: "report-erp",
    name: "Tổng quan triển khai ERP Q4",
    scope: { id: "space-tech", label: "Phòng Công nghệ", tone: "blue" },
    owner: PEOPLE.phuc,
    access: "edit",
    mine: false,
    sourceBoardName: ERP_BOARD.name,
    multiSource: false,
    widgetCount: 6,
    sharedWith: ["Phòng Công nghệ", "Ban giám đốc"],
    includesViewer: false,
    excludedBoardCount: 0,
    updatedAt: "2026-10-12T03:38:00.000Z",
  },
  {
    id: "report-close",
    name: "Đóng sổ tháng 10",
    scope: { id: "space-acc", label: "Phòng Kế toán", tone: "gold" },
    owner: PEOPLE.linh,
    access: "view",
    mine: false,
    sourceBoardName: "Đóng sổ tháng 10",
    multiSource: false,
    widgetCount: null,
    sharedWith: ["Phòng Kế toán"],
    includesViewer: true,
    excludedBoardCount: 0,
    updatedAt: "2026-10-12T03:35:00.000Z",
  },
  {
    id: "report-weekly",
    name: "Báo cáo tuần Ban giám đốc",
    scope: { id: "space-exec", label: "Ban giám đốc", tone: "crimson" },
    owner: PEOPLE.hung,
    access: "view",
    mine: false,
    sourceBoardName: null,
    multiSource: true,
    widgetCount: null,
    sharedWith: ["Ban giám đốc"],
    includesViewer: true,
    excludedBoardCount: 1,
    updatedAt: "2026-10-12T03:36:00.000Z",
  },
];

const LINK = (href: string) => ({ href, onNavigate: noop });
export const REPORT_LINKS: ReportLinks = {
  list: LINK("/workspaces/w/reports"),
  report: (id) => LINK(`/workspaces/w/reports?report=${id}`),
  board: (id) => LINK(`/workspaces/w/boards/${id}?view=table`),
  item: (boardId, id) => LINK(`/workspaces/w/boards/${boardId}?item=${id}`),
};

export const WIDGET_OPTIONS: ReportWidgetOptionsDto = {
  boards: [
    {
      id: ERP_BOARD.id,
      name: ERP_BOARD.name,
      dimensions: ["assignee", "status", "priority", "module", "week"],
    },
    {
      id: "board-close",
      name: "Đóng sổ tháng 10",
      dimensions: ["assignee", "status", "priority", "week"],
    },
  ],
  metrics: ["count", "overdue"],
  filters: ["not-done", "overdue", "assigned", "unassigned", "high-priority"],
};

/** The preview of "Việc chưa hoàn thành theo người phụ trách" (Cột). */
export function previewColumns(): ReportWidgetDto {
  return {
    id: "preview",
    size: "wide",
    title: null,
    titleKey: null,
    staleAsOf: null,
    spec: spec("preview", "columns", "wide", { groupBy: "assignee" }),
    kind: "columns",
    groupBy: "assignee",
    metric: "count",
    boardName: ERP_BOARD.name,
    total: 11,
    max: 2,
    rows: [
      ...WORKLOAD.map(([person, count]) =>
        row(person.id, person.name, count, { person }),
      ),
      row("none", "", 1, { none: true }),
    ],
  };
}

export function listScenario(
  overrides: Partial<ReportListSurfaceProps> = {},
): ReportListSurfaceProps {
  return {
    copy: REPORTS_FIXTURE_COPY,
    formatters: REPORTS_FIXTURE_FORMATTERS,
    links: REPORT_LINKS,
    workspaceName: "Minh An Group",
    state: { status: "ready", data: REPORT_LIST },
    now: "2026-10-12T03:40:00.000Z",
    timeZone: TIME_ZONE,
    filter: "all",
    onFilter: noop,
    creating: false,
    createFailed: false,
    onCreate: noop,
    onRetry: noop,
    ...overrides,
  };
}

const ok = () => Promise.resolve(true);

export function dashboardScenario(
  overrides: Partial<ReportDashboardSurfaceProps> = {},
): ReportDashboardSurfaceProps {
  return {
    copy: REPORTS_FIXTURE_COPY,
    formatters: REPORTS_FIXTURE_FORMATTERS,
    links: REPORT_LINKS,
    report: erpReport(),
    assigneeId: null,
    onAssignee: noop,
    reloading: false,
    onReload: noop,
    editing: false,
    onStartEdit: noop,
    onCancelEdit: noop,
    addOpen: false,
    onOpenAdd: noop,
    onCloseAdd: noop,
    drillKey: null,
    onDrill: noop,
    exportOpen: false,
    onOpenExport: noop,
    onCloseExport: noop,
    drill: { status: "loading" },
    onRetryDrill: noop,
    widgetOptions: { status: "ready", data: WIDGET_OPTIONS },
    onRetryOptions: noop,
    onPreviewWidget: () => Promise.resolve(previewColumns()),
    sharing: {
      status: "ready",
      data: {
        targets: [
          { id: "space-tech", label: "Phòng Công nghệ", tone: "blue" },
          { id: "space-acc", label: "Phòng Kế toán", tone: "gold" },
          { id: "space-exec", label: "Ban giám đốc", tone: "crimson" },
        ],
        selected: ["space-tech", "space-exec"],
        canEdit: true,
      },
    },
    exportJob: null,
    onCheckExport: noop,
    onStartExport: ok,
    onSaveLayout: ok,
    onRemoveWidget: ok,
    onRename: ok,
    onShare: ok,
    onDelete: ok,
    onApplyLayout: ok,
    ...overrides,
  };
}

export const DRILL_QUAN = {
  status: "ready" as const,
  data: {
    person: PEOPLE.quan,
    items: QUAN_ITEMS,
    board: ERP_BOARD,
    dataAsOf: "2026-10-12T03:37:00.000Z",
    timeZone: TIME_ZONE,
  },
};
