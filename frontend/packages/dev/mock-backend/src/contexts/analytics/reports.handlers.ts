/**
 * Analytics — the Reports pages of the 2026-10 Reports mockup (gap
 * `CTR-GAP-REPORTS`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Reports"):
 *
 *   reports.list           — GET    /workspaces/:workspaceId/reports
 *   reports.get            — GET    /workspaces/:workspaceId/reports/:reportId
 *   reports.create         — POST   /workspaces/:workspaceId/reports
 *   reports.rename         — PATCH  /workspaces/:workspaceId/reports/:reportId
 *   reports.delete         — DELETE /workspaces/:workspaceId/reports/:reportId
 *   reports.widgets.save   — PUT    /workspaces/:workspaceId/reports/:reportId/widgets
 *   reports.layout.apply   — POST   /workspaces/:workspaceId/reports/:reportId/layout
 *   reports.refresh        — POST   /workspaces/:workspaceId/reports/:reportId/refresh
 *   reports.drilldown      — GET    /workspaces/:workspaceId/reports/:reportId/drilldown
 *   reports.sharing.get/save — GET/PUT /workspaces/:workspaceId/reports/:reportId/sharing
 *   reports.export.start   — POST   /workspaces/:workspaceId/reports/:reportId/exports
 *   reports.export.get     — GET    /workspaces/:workspaceId/report-exports/:jobId
 *   reports.widgetOptions  — GET    /workspaces/:workspaceId/report-widget-options
 *   reports.widgetPreview  — POST   /workspaces/:workspaceId/report-widget-preview
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  FILTER_IDS,
  REPORTS_TIME_ZONE,
  REPORT_IDS,
  SHARE_TARGETS,
  boardItems,
  csvOf,
  drillDown,
  groupItems,
  hanoiDay,
  personOf,
  presetWidgets,
  reportableBoards,
  summaryOf,
  widgetData,
  type ExportJob,
  type FilterId,
  type GroupBy,
  type LayoutId,
  type Metric,
  type StoredReport,
  type WidgetContext,
  type WidgetKind,
  type WidgetSize,
  type WidgetSpec,
} from "../../state/mock-reports";
import { mockIds } from "../../state/mock-ids";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-REPORTS" } as const;

const KINDS: readonly WidgetKind[] = [
  "status",
  "velocity",
  "module",
  "priority",
  "due",
  "workload",
  "dept-done",
  "dept-overdue",
  "note",
  "erp-progress",
  "number",
  "columns",
  "line",
  "donut",
  "table",
  "progress",
];
const SIZES: readonly WidgetSize[] = ["wide", "half", "side"];
const GROUPS: readonly GroupBy[] = [
  "assignee",
  "status",
  "priority",
  "module",
  "week",
];
const METRICS: readonly Metric[] = ["count", "overdue"];
const LAYOUTS: readonly LayoutId[] = ["project", "team", "deadline"];
const NAME_MAX = 80;
const NOTE_MAX = 600;

const TITLE_VI: Readonly<Record<string, string>> = {
  status: "Theo trạng thái",
  velocity: "Tốc độ hoàn thành",
  module: "Theo phân hệ",
  priority: "Theo mức ưu tiên",
  due: "Đến hạn trong 7 ngày",
  workload: "Khối lượng theo người phụ trách",
  deptDone: "Việc hoàn thành theo phòng",
  deptOverdue: "Việc quá hạn theo phòng",
  note: "Ghi chú",
  erpProgress: "Tiến độ Triển khai ERP Q4",
};

function contextOf(
  store: MockStore,
  report: StoredReport,
  assigneeId: string | null,
): WidgetContext {
  return {
    store,
    report,
    nowMs: store.getClock().now().getTime(),
    dataAsOf: report.dataAsOf,
    assigneeId,
    viewerId: store.getCurrentUser().id,
  };
}

const accessOf = (store: MockStore, report: StoredReport) =>
  report.editorIds.includes(store.getCurrentUser().id) ? "edit" : "view";

function listItem(store: MockStore, report: StoredReport) {
  const viewerId = store.getCurrentUser().id;
  const board = report.sourceBoardId
    ? store.getBoard(report.sourceBoardId)
    : undefined;
  return {
    id: report.id,
    name: report.name,
    scope: {
      id: report.scopeId,
      label: report.scopeLabel,
      tone: report.scopeTone,
    },
    owner: personOf(store, report.ownerId),
    access: accessOf(store, report),
    mine: report.ownerId === viewerId,
    sourceBoardName: board?.title ?? null,
    multiSource: report.multiSource,
    widgetCount: report.widgetCountShown ? report.widgets.length : null,
    sharedWith: report.sharedWith,
    includesViewer: report.includesViewer,
    excludedBoardCount: report.excludedBoardCount,
    updatedAt: report.updatedAt,
  };
}

function assigneeOptions(store: MockStore, boardId: string) {
  const rows = groupItems(
    store,
    boardItems(store, boardId).filter((item) => item.bucket !== "done"),
    "assignee",
    boardId,
    store.getClock().now().getTime(),
  );
  return rows
    .filter((row) => row.person)
    .map((row) => ({ id: row.person!.id, name: row.person!.name }));
}

function filtersOf(store: MockStore, report: StoredReport) {
  const boardId = report.sourceBoardId;
  const board = boardId ? store.getBoard(boardId) : undefined;
  const isErp = boardId === mockIds.boards.erp;
  return {
    board: board
      ? {
          selectedId: board.id,
          options: [{ id: board.id, name: board.title }],
        }
      : null,
    time: isErp
      ? { kind: "until" as const, day: "2026-11-01" }
      : report.multiSource
        ? {
            kind: "week" as const,
            from: "2026-10-05",
            to: "2026-10-11",
          }
        : null,
    assignee: boardId ? { options: assigneeOptions(store, boardId) } : null,
    sources: report.multiSource
      ? {
          departments: [
            "Kế toán",
            "Marketing",
            "Nhân sự",
            "Công nghệ",
            "Ban giám đốc",
          ],
        }
      : null,
  };
}

function reportBody(
  store: MockStore,
  report: StoredReport,
  assigneeId: string | null,
) {
  const ctx = contextOf(store, report, assigneeId);
  const boardId = report.sourceBoardId;
  const summaryBoard =
    boardId && boardId === mockIds.boards.erp ? boardId : null;
  return {
    id: report.id,
    name: report.name,
    scope: {
      id: report.scopeId,
      label: report.scopeLabel,
      tone: report.scopeTone,
    },
    owner: personOf(store, report.ownerId),
    sharedWith: report.sharedWith,
    includesViewer: report.includesViewer,
    access: accessOf(store, report),
    createdAt: report.createdAt,
    updatedAt: report.updatedAt,
    dataAsOf: report.dataAsOf,
    timeZone: REPORTS_TIME_ZONE,
    excludedBoardCount: report.excludedBoardCount,
    sourceBoard: boardId
      ? {
          id: boardId,
          name: store.getBoard(boardId)?.title ?? "",
        }
      : null,
    filters: filtersOf(store, report),
    summary: summaryBoard ? summaryOf(ctx, summaryBoard) : null,
    widgets: report.widgets.map((spec) => widgetData(spec, ctx)),
  };
}

const text = (value: unknown, max: number): string | null => {
  if (typeof value !== "string") return null;
  const trimmed = value.trim();
  return trimmed.length > 0 && trimmed.length <= max ? trimmed : null;
};

function parseSpec(
  raw: unknown,
  store: MockStore,
  workspaceId: string,
  viewerId: string,
  todayMs: number,
): WidgetSpec | null {
  if (typeof raw !== "object" || raw === null) return null;
  const input = raw as Record<string, unknown>;
  const kind = input.kind as WidgetKind;
  if (!KINDS.includes(kind)) return null;
  const size = SIZES.includes(input.size as WidgetSize)
    ? (input.size as WidgetSize)
    : "wide";
  const boards = reportableBoards(store, workspaceId).map((b) => b.id);
  const boardId =
    typeof input.boardId === "string" && boards.includes(input.boardId)
      ? input.boardId
      : null;
  const filters = Array.isArray(input.filters)
    ? (input.filters.filter((f) =>
        FILTER_IDS.includes(f as FilterId),
      ) as FilterId[])
    : [];
  const noteText = kind === "note" ? text(input.text, NOTE_MAX) : null;
  return {
    id:
      typeof input.id === "string" && input.id.length <= 60
        ? input.id
        : `mock-widget-${todayMs}`,
    kind,
    size,
    title: text(input.title, NAME_MAX),
    titleKey: typeof input.titleKey === "string" ? input.titleKey : null,
    boardId,
    metric: METRICS.includes(input.metric as Metric)
      ? (input.metric as Metric)
      : "count",
    groupBy: GROUPS.includes(input.groupBy as GroupBy)
      ? (input.groupBy as GroupBy)
      : "assignee",
    filters,
    text: noteText,
    authorId:
      kind === "note"
        ? typeof input.authorId === "string"
          ? input.authorId
          : viewerId
        : null,
    writtenOn:
      kind === "note"
        ? typeof input.writtenOn === "string"
          ? input.writtenOn
          : hanoiDay(todayMs)
        : null,
  };
}

function minimalPdf(lines: readonly string[]): string {
  const esc = (s: string) =>
    s
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .replace(/đ/g, "d")
      .replace(/Đ/g, "D")
      .replace(/[()\\]/g, "");
  const stream = `BT /F1 12 Tf 40 780 Td 16 TL ${lines
    .map((line) => `(${esc(line)}) Tj T*`)
    .join(" ")} ET`;
  const objects = [
    "<< /Type /Catalog /Pages 2 0 R >>",
    "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    `<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`,
  ];
  let out = "%PDF-1.4\n";
  const offsets: number[] = [];
  objects.forEach((body, index) => {
    offsets.push(out.length);
    out += `${index + 1} 0 obj\n${body}\nendobj\n`;
  });
  const xref = out.length;
  out += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  for (const offset of offsets)
    out += `${String(offset).padStart(10, "0")} 00000 n \n`;
  out += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF`;
  return out;
}

function exportContent(
  store: MockStore,
  report: StoredReport,
  format: "pdf" | "csv",
): string {
  const ctx = contextOf(store, report, null);
  const name = report.name ?? "Báo cáo chưa đặt tên";
  const widgets = report.widgets.map((spec) => {
    const data = widgetData(spec, ctx) as unknown as Record<string, unknown>;
    const title =
      (data.title as string | null) ??
      TITLE_VI[(data.titleKey as string | null) ?? ""] ??
      String(data.kind);
    const rows = (
      data.rows as
        | { label: string; count: number; person?: { name: string } | null }[]
        | undefined
    )?.map((row) => [row.person?.name ?? row.label, row.count]) ??
      (data.items as { key: string; title: string }[] | undefined)?.map(
        (item) => [item.key, item.title],
      ) ?? [[String(data.kind), String(data.total ?? data.done ?? "")]];
    return { title, rows: rows as (string | number)[][] };
  });
  if (format === "csv") return csvOf(name, widgets);
  return minimalPdf([
    name,
    `So lieu den ${report.dataAsOf}`,
    "",
    ...widgets.flatMap((widget) => [
      widget.title,
      ...widget.rows.map((row) => `  ${row.join("  ")}`),
      "",
    ]),
  ]);
}

const slugOf = (name: string) =>
  name
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "") || "bao-cao";

type Params = { workspaceId: string; reportId: string };

export const reportsOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "reports.list",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/reports",
    async handle({ store }) {
      return ok({
        serverTime: store.getClock().isoNow(),
        reports: store.reports.list().map((report) => listItem(store, report)),
      });
    },
  }),

  defineMockOperation<Params, never, unknown>({
    id: "reports.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/reports/:reportId",
    async handle({ params, request, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      const assignee = request.searchParams.get("assignee");
      return ok(reportBody(store, report, assignee || null));
    },
  }),

  defineMockOperation<{ workspaceId: string }, unknown, unknown>({
    id: "reports.create",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/reports",
    async handle({ store }) {
      const viewer = store.getCurrentUser();
      const now = store.getClock().isoNow();
      const id = store.reports.nextId("mock-report-new");
      const scope =
        SHARE_TARGETS.find(
          (target) => target.id === mockIds.spaces.engineering,
        ) ?? SHARE_TARGETS[0]!;
      const report: StoredReport = {
        id,
        name: null,
        scopeId: scope.id,
        scopeLabel: scope.label,
        scopeTone: scope.tone,
        ownerId: viewer.id,
        sharedWith: [],
        includesViewer: false,
        editorIds: [viewer.id],
        createdAt: now,
        updatedAt: now,
        dataAsOf: now,
        sourceBoardId: null,
        multiSource: false,
        excludedBoardCount: 0,
        widgetCountShown: true,
        recomputed: true,
        widgets: [],
      };
      store.reports.put(report);
      return created(reportBody(store, report, null));
    },
  }),

  defineMockOperation<Params, { name?: unknown }, unknown>({
    id: "reports.rename",
    contract: gap,
    method: "PATCH",
    route: "/workspaces/:workspaceId/reports/:reportId",
    async handle({ params, body, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (accessOf(store, report) !== "edit") return forbidden();
      const name = text(body?.name, NAME_MAX);
      if (!name) return validationError({ name: ["invalid"] });
      store.reports.put({
        ...report,
        name,
        updatedAt: store.getClock().isoNow(),
      });
      return ok(reportBody(store, store.reports.get(report.id)!, null));
    },
  }),

  defineMockOperation<Params, never, unknown>({
    id: "reports.delete",
    contract: gap,
    method: "DELETE",
    route: "/workspaces/:workspaceId/reports/:reportId",
    async handle({ params, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (accessOf(store, report) !== "edit") return forbidden();
      store.reports.remove(report.id);
      return noContent();
    },
  }),

  defineMockOperation<Params, { widgets?: unknown }, unknown>({
    id: "reports.widgets.save",
    contract: gap,
    method: "PUT",
    route: "/workspaces/:workspaceId/reports/:reportId/widgets",
    async handle({ params, body, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (accessOf(store, report) !== "edit") return forbidden();
      if (!Array.isArray(body?.widgets) || body.widgets.length > 24)
        return validationError({ widgets: ["invalid"] });
      const viewerId = store.getCurrentUser().id;
      const nowMs = store.getClock().now().getTime();
      const specs: WidgetSpec[] = [];
      const seen = new Set<string>();
      for (const raw of body.widgets) {
        const spec = parseSpec(raw, store, params.workspaceId, viewerId, nowMs);
        if (!spec || seen.has(spec.id))
          return validationError({ widgets: ["invalid"] });
        seen.add(spec.id);
        specs.push(spec);
      }
      const boardIds = [
        ...new Set(
          specs
            .map((spec) => spec.boardId ?? report.sourceBoardId)
            .filter((id): id is string => !!id),
        ),
      ];
      const now = store.getClock().isoNow();
      store.reports.put({
        ...report,
        widgets: specs,
        sourceBoardId:
          report.multiSource || boardIds.length !== 1
            ? report.sourceBoardId
            : boardIds[0]!,
        updatedAt: now,
        dataAsOf: now,
      });
      return ok(reportBody(store, store.reports.get(report.id)!, null));
    },
  }),

  defineMockOperation<Params, { layout?: unknown }, unknown>({
    id: "reports.layout.apply",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/reports/:reportId/layout",
    async handle({ params, body, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (accessOf(store, report) !== "edit") return forbidden();
      if (!LAYOUTS.includes(body?.layout as LayoutId))
        return validationError({ layout: ["invalid"] });
      const boards = reportableBoards(store, params.workspaceId);
      const preferred =
        report.sourceBoardId ??
        (report.scopeId === mockIds.spaces.accounting
          ? mockIds.boards.accounting
          : mockIds.boards.erp);
      const boardId =
        boards.find((b) => b.id === preferred)?.id ?? boards[0]?.id;
      if (!boardId) return validationError({ layout: ["no-board"] });
      let counter = 0;
      const widgets = presetWidgets(
        body.layout as LayoutId,
        boardId,
        () => `${report.id}-w${(counter += 1)}`,
      );
      const now = store.getClock().isoNow();
      store.reports.put({
        ...report,
        widgets,
        sourceBoardId: boardId,
        updatedAt: now,
        dataAsOf: now,
      });
      return ok(reportBody(store, store.reports.get(report.id)!, null));
    },
  }),

  defineMockOperation<Params, unknown, unknown>({
    id: "reports.refresh",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/reports/:reportId/refresh",
    async handle({ params, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      store.reports.put({
        ...report,
        recomputed: true,
        dataAsOf: store.getClock().isoNow(),
      });
      return ok(reportBody(store, store.reports.get(report.id)!, null));
    },
  }),

  defineMockOperation<Params, never, unknown>({
    id: "reports.drilldown",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/reports/:reportId/drilldown",
    async handle({ params, request, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      const assignee = request.searchParams.get("assignee");
      const boardId = report.sourceBoardId;
      if (!assignee || !boardId)
        return validationError({ assignee: ["required"] });
      const result = drillDown(
        contextOf(store, report, null),
        boardId,
        assignee,
      );
      if (!result) return notFound();
      return ok({
        person: result.person,
        items: result.items,
        board: { id: boardId, name: store.getBoard(boardId)?.title ?? "" },
        dataAsOf: report.dataAsOf,
        timeZone: REPORTS_TIME_ZONE,
      });
    },
  }),

  defineMockOperation<Params, never, unknown>({
    id: "reports.sharing.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/reports/:reportId/sharing",
    async handle({ params, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      return ok({
        targets: SHARE_TARGETS,
        selected: SHARE_TARGETS.filter((target) =>
          report.sharedWith.includes(target.label),
        ).map((target) => target.id),
        canEdit: accessOf(store, report) === "edit",
      });
    },
  }),

  defineMockOperation<Params, { scopeIds?: unknown }, unknown>({
    id: "reports.sharing.save",
    contract: gap,
    method: "PUT",
    route: "/workspaces/:workspaceId/reports/:reportId/sharing",
    async handle({ params, body, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (accessOf(store, report) !== "edit") return forbidden();
      if (!Array.isArray(body?.scopeIds))
        return validationError({ scopeIds: ["invalid"] });
      const ids = new Set(body.scopeIds as string[]);
      store.reports.put({
        ...report,
        sharedWith: SHARE_TARGETS.filter((target) => ids.has(target.id)).map(
          (target) => target.label,
        ),
        updatedAt: store.getClock().isoNow(),
      });
      return ok(reportBody(store, store.reports.get(report.id)!, null));
    },
  }),

  defineMockOperation<Params, { format?: unknown }, unknown>({
    id: "reports.export.start",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/reports/:reportId/exports",
    async handle({ params, body, store }) {
      const report = store.reports.get(params.reportId);
      if (!report) return notFound();
      if (body?.format !== "pdf" && body?.format !== "csv")
        return validationError({ format: ["invalid"] });
      const job: ExportJob = {
        id: store.reports.nextId("mock-report-export"),
        reportId: report.id,
        format: body.format,
        // The file starts a minute after the data snapshot of the mockup.
        startedAt: store.getClock().offsetMinutes(1),
        checks: 0,
        status: "running",
        filename: `${slugOf(report.name ?? "bao-cao")}.${body.format}`,
        content: "",
      };
      store.reports.addJob(job);
      return created({
        id: job.id,
        format: job.format,
        status: job.status,
        startedAt: job.startedAt,
        filename: job.filename,
        downloadUrl: null,
        expiresAt: null,
      });
    },
  }),

  defineMockOperation<{ workspaceId: string; jobId: string }, never, unknown>({
    id: "reports.export.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/report-exports/:jobId",
    async handle({ params, store }) {
      const job = store.reports.job(params.jobId);
      if (!job) return notFound();
      const report = store.reports.get(job.reportId);
      if (!report) return notFound();
      job.checks += 1;
      if (job.status === "running" && job.checks >= 2) {
        job.status = "done";
        job.content = exportContent(store, report, job.format);
        const user = store.getCurrentUser();
        store.pushNotification({
          id: `mock-notification-${job.id}`,
          workspaceId: params.workspaceId,
          userId: user.id,
          type: "system",
          title: "Báo cáo đã xuất xong",
          body: `${report.name ?? "Báo cáo"} · ${job.format.toUpperCase()} · liên kết tải xuống hết hạn sau 7 ngày`,
          link: `/workspaces/${params.workspaceId}/reports?report=${report.id}`,
          isRead: false,
          isArchived: false,
          createdAt: store.getClock().isoNow(),
        });
      }
      const done = job.status === "done";
      const mime = job.format === "pdf" ? "application/pdf" : "text/csv";
      return ok({
        id: job.id,
        format: job.format,
        status: job.status,
        startedAt: job.startedAt,
        filename: job.filename,
        downloadUrl: done
          ? `data:${mime};charset=utf-8,${encodeURIComponent(job.content)}`
          : null,
        expiresAt: done ? store.getClock().offsetDays(7) : null,
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "reports.widgetOptions",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/report-widget-options",
    async handle({ params, store }) {
      return ok({
        boards: reportableBoards(store, params.workspaceId),
        metrics: METRICS,
        filters: FILTER_IDS,
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, { spec?: unknown }, unknown>({
    id: "reports.widgetPreview",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/report-widget-preview",
    async handle({ params, body, store }) {
      const nowMs = store.getClock().now().getTime();
      const spec = parseSpec(
        { ...(body?.spec as object), id: "preview" },
        store,
        params.workspaceId,
        store.getCurrentUser().id,
        nowMs,
      );
      if (!spec) return validationError({ spec: ["invalid"] });
      const report = store.reports.get(REPORT_IDS.erp);
      if (!report) return notFound();
      const preview = { ...report, sourceBoardId: spec.boardId ?? null };
      return ok({
        widget: widgetData(spec, contextOf(store, preview, null)),
        dataAsOf: report.dataAsOf,
      });
    },
  }),
];
