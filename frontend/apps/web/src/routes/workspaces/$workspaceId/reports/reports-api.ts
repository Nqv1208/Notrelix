import type {
  ReportDrillDto,
  ReportDto,
  ReportExportJobDto,
  ReportLayoutId,
  ReportListItemDto,
  ReportSharingDto,
  ReportWidgetDto,
  ReportWidgetOptionsDto,
  ReportWidgetSpec,
} from "@notrelix/features-workspace/web";

/**
 * The calls behind the Reports pages (gap `CTR-GAP-REPORTS`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Reports"). `api` is the runtime client; every call is quiet: the pages say
 * what went wrong themselves.
 */
export interface ReportsApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; idempotencyKey?: string },
  ): Promise<T>;
  put<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  patch<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;

const root = (workspaceId: string) =>
  `/workspaces/${encodeURIComponent(workspaceId)}`;
const reportPath = (workspaceId: string, reportId: string) =>
  `${root(workspaceId)}/reports/${encodeURIComponent(reportId)}`;

export function createReportsApi(api: ReportsApiClient) {
  return {
    list(workspaceId: string) {
      return api.get<{ serverTime: string; reports: ReportListItemDto[] }>(
        `${root(workspaceId)}/reports`,
        QUIET,
      );
    },
    get(workspaceId: string, reportId: string, assigneeId: string | null) {
      const query = assigneeId
        ? `?assignee=${encodeURIComponent(assigneeId)}`
        : "";
      return api.get<ReportDto>(
        `${reportPath(workspaceId, reportId)}${query}`,
        QUIET,
      );
    },
    create(workspaceId: string) {
      return api.post<ReportDto>(
        `${root(workspaceId)}/reports`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    rename(workspaceId: string, reportId: string, name: string) {
      return api.patch<ReportDto>(
        reportPath(workspaceId, reportId),
        { name },
        QUIET,
      );
    },
    remove(workspaceId: string, reportId: string) {
      return api.delete<void>(reportPath(workspaceId, reportId), QUIET);
    },
    saveWidgets(
      workspaceId: string,
      reportId: string,
      widgets: readonly ReportWidgetSpec[],
    ) {
      return api.put<ReportDto>(
        `${reportPath(workspaceId, reportId)}/widgets`,
        { widgets },
        QUIET,
      );
    },
    applyLayout(workspaceId: string, reportId: string, layout: ReportLayoutId) {
      return api.post<ReportDto>(
        `${reportPath(workspaceId, reportId)}/layout`,
        { layout },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    refresh(workspaceId: string, reportId: string) {
      return api.post<ReportDto>(
        `${reportPath(workspaceId, reportId)}/refresh`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    drilldown(workspaceId: string, reportId: string, assignee: string) {
      return api.get<ReportDrillDto>(
        `${reportPath(workspaceId, reportId)}/drilldown?assignee=${encodeURIComponent(assignee)}`,
        QUIET,
      );
    },
    getSharing(workspaceId: string, reportId: string) {
      return api.get<ReportSharingDto>(
        `${reportPath(workspaceId, reportId)}/sharing`,
        QUIET,
      );
    },
    saveSharing(
      workspaceId: string,
      reportId: string,
      scopeIds: readonly string[],
    ) {
      return api.put<ReportDto>(
        `${reportPath(workspaceId, reportId)}/sharing`,
        { scopeIds },
        QUIET,
      );
    },
    startExport(workspaceId: string, reportId: string, format: "pdf" | "csv") {
      return api.post<ReportExportJobDto>(
        `${reportPath(workspaceId, reportId)}/exports`,
        { format },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    getExport(workspaceId: string, jobId: string) {
      return api.get<ReportExportJobDto>(
        `${root(workspaceId)}/report-exports/${encodeURIComponent(jobId)}`,
        QUIET,
      );
    },
    widgetOptions(workspaceId: string) {
      return api.get<ReportWidgetOptionsDto>(
        `${root(workspaceId)}/report-widget-options`,
        QUIET,
      );
    },
    previewWidget(workspaceId: string, spec: ReportWidgetSpec) {
      return api.post<{ widget: ReportWidgetDto; dataAsOf: string }>(
        `${root(workspaceId)}/report-widget-preview`,
        { spec },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
  };
}

export type ReportsApi = ReturnType<typeof createReportsApi>;
