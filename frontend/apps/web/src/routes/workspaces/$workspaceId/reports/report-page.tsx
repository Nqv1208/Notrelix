import { useCallback, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  ReportDashboardSurface,
  useReportsCopy,
  type ReportDrillState,
  type ReportDto,
  type ReportExportJobDto,
  type ReportLayoutId,
  type ReportSharingState,
  type ReportWidgetOptionsState,
  type ReportWidgetSpec,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import type { ReportsSearch } from "../../../../router/schemas/reports-search-schema";
import { reportsKeys, useReportLinks, useReportsApi } from "./use-reports";

const ROUTE = "/workspaces/$workspaceId/reports";

/** One report: the dashboard, its editor, drill-down, export and sharing. */
export function ReportPage({ reportId }: { reportId: string }) {
  const { workspaceId } = useWorkspaceContext();
  const copy = useReportsCopy();
  const formatters = useFormatters();
  const api = useReportsApi();
  const links = useReportLinks();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const search: ReportsSearch = useSearch({ from: ROUTE });
  const assigneeId = search.assignee ?? null;
  const adding = search.add === true;
  const editing = search.edit === true || adding;
  const [exportJobId, setExportJobId] = useState<string | null>(null);
  const [reloading, setReloading] = useState(false);

  const patchSearch = useCallback(
    (patch: Partial<ReportsSearch>, replace = false) =>
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: (previous: ReportsSearch) => ({ ...previous, ...patch }),
        replace,
      } as never),
    [navigate, workspaceId],
  );

  const report = useQuery({
    queryKey: reportsKeys.report(workspaceId, reportId, assigneeId),
    queryFn: () => api.get(workspaceId, reportId, assigneeId),
    retry: false,
  });
  const sharing = useQuery({
    queryKey: reportsKeys.sharing(workspaceId, reportId),
    queryFn: () => api.getSharing(workspaceId, reportId),
    enabled: report.isSuccess,
    retry: false,
  });
  const options = useQuery({
    queryKey: reportsKeys.options(workspaceId),
    queryFn: () => api.widgetOptions(workspaceId),
    enabled: report.isSuccess && editing,
    retry: false,
  });
  const drillKey = search.drill ?? null;
  const drillQuery = useQuery({
    queryKey: reportsKeys.drill(workspaceId, reportId, drillKey ?? ""),
    queryFn: () => api.drilldown(workspaceId, reportId, drillKey ?? ""),
    enabled: report.isSuccess && drillKey !== null && !editing,
    retry: false,
  });
  const job = useQuery({
    queryKey: reportsKeys.exportJob(workspaceId, exportJobId ?? ""),
    queryFn: () => api.getExport(workspaceId, exportJobId ?? ""),
    enabled: exportJobId !== null,
    retry: false,
    refetchInterval: (query) =>
      query.state.data?.status === "running" ? 3000 : false,
  });

  const setReport = useCallback(
    (next: ReportDto) => {
      queryClient.setQueryData(
        reportsKeys.report(workspaceId, reportId, assigneeId),
        next,
      );
      void queryClient.invalidateQueries({
        queryKey: reportsKeys.list(workspaceId),
      });
    },
    [assigneeId, queryClient, reportId, workspaceId],
  );

  const drill = useMemo<ReportDrillState>(
    () =>
      drillQuery.data
        ? { status: "ready", data: drillQuery.data }
        : drillQuery.isError
          ? { status: "error" }
          : { status: "loading" },
    [drillQuery.data, drillQuery.isError],
  );
  const widgetOptions = useMemo<ReportWidgetOptionsState>(
    () =>
      options.data
        ? { status: "ready", data: options.data }
        : options.isError
          ? { status: "error" }
          : { status: "loading" },
    [options.data, options.isError],
  );
  const sharingState = useMemo<ReportSharingState>(
    () =>
      sharing.data
        ? { status: "ready", data: sharing.data }
        : sharing.isError
          ? { status: "error" }
          : { status: "loading" },
    [sharing.data, sharing.isError],
  );

  const attempt = useCallback(
    async (action: () => Promise<unknown>): Promise<boolean> => {
      try {
        await action();
        return true;
      } catch {
        return false;
      }
    },
    [],
  );

  const onSaveLayout = useCallback(
    (widgets: readonly ReportWidgetSpec[]) =>
      attempt(async () => {
        setReport(await api.saveWidgets(workspaceId, reportId, widgets));
        patchSearch({ edit: undefined, add: undefined });
        toast.success(copy.edit.saved);
      }),
    [api, attempt, copy.edit.saved, patchSearch, reportId, setReport, workspaceId],
  );

  const onRemoveWidget = useCallback(
    (widgetId: string) =>
      attempt(async () => {
        const current = report.data;
        if (!current) throw new Error("no report");
        setReport(
          await api.saveWidgets(
            workspaceId,
            reportId,
            current.widgets
              .filter((widget) => widget.id !== widgetId)
              .map((widget) => ({ ...widget.spec, size: widget.size })),
          ),
        );
      }),
    [api, attempt, report.data, reportId, setReport, workspaceId],
  );

  const onRename = useCallback(
    (name: string) =>
      attempt(async () => {
        setReport(await api.rename(workspaceId, reportId, name));
      }),
    [api, attempt, reportId, setReport, workspaceId],
  );

  const onShare = useCallback(
    (scopeIds: readonly string[]) =>
      attempt(async () => {
        setReport(await api.saveSharing(workspaceId, reportId, scopeIds));
        await queryClient.invalidateQueries({
          queryKey: reportsKeys.sharing(workspaceId, reportId),
        });
        toast.success(copy.share.saved);
      }),
    [api, attempt, copy.share.saved, queryClient, reportId, setReport, workspaceId],
  );

  const onDelete = useCallback(
    () =>
      attempt(async () => {
        await api.remove(workspaceId, reportId);
        await queryClient.invalidateQueries({
          queryKey: reportsKeys.all(workspaceId),
        });
        toast.success(copy.remove.done);
        void navigate({
          to: ROUTE,
          params: { workspaceId },
          search: {},
        } as never);
      }),
    [api, attempt, copy.remove.done, navigate, queryClient, reportId, workspaceId],
  );

  const onApplyLayout = useCallback(
    (layout: ReportLayoutId) =>
      attempt(async () => {
        setReport(await api.applyLayout(workspaceId, reportId, layout));
      }),
    [api, attempt, reportId, setReport, workspaceId],
  );

  const onStartExport = useCallback(
    (format: "pdf" | "csv") =>
      attempt(async () => {
        const started = await api.startExport(workspaceId, reportId, format);
        queryClient.setQueryData(
          reportsKeys.exportJob(workspaceId, started.id),
          started,
        );
        setExportJobId(started.id);
      }),
    [api, attempt, queryClient, reportId, workspaceId],
  );

  const onReload = useCallback(() => {
    setReloading(true);
    void attempt(async () => {
      setReport(await api.refresh(workspaceId, reportId));
    }).finally(() => setReloading(false));
  }, [api, attempt, reportId, setReport, workspaceId]);

  const onPreviewWidget = useCallback(
    async (spec: ReportWidgetSpec) =>
      (await api.previewWidget(workspaceId, spec)).widget,
    [api, workspaceId],
  );

  if (report.isError) {
    return (
      <div className="mx-auto flex max-w-xl flex-col items-start gap-3 px-8 py-12">
        <h1 className="m-0 text-page-title font-[650]">
          {copy.page.notFoundTitle}
        </h1>
        <p className="m-0 text-body text-muted-foreground">
          {copy.page.notFoundBody}
        </p>
        <a
          href={links.list.href}
          onClick={(event) => {
            event.preventDefault();
            links.list.onNavigate();
          }}
          className="text-body font-semibold text-brand-ink hover:underline"
        >
          {copy.page.back}
        </a>
        <button
          type="button"
          onClick={() => void report.refetch()}
          className="text-body font-semibold text-brand-ink hover:underline"
        >
          {copy.page.retry}
        </button>
      </div>
    );
  }
  if (!report.data) {
    return (
      <p role="status" className="px-8 py-6 text-body text-muted-foreground">
        {copy.page.loading}
      </p>
    );
  }

  const exportJob: ReportExportJobDto | null = job.data ?? null;
  return (
    <ReportDashboardSurface
      copy={copy}
      formatters={formatters}
      links={links}
      report={report.data}
      assigneeId={assigneeId}
      onAssignee={(id) => patchSearch({ assignee: id ?? undefined }, true)}
      reloading={reloading || report.isFetching}
      onReload={onReload}
      editing={editing}
      onStartEdit={() => patchSearch({ edit: true, drill: undefined })}
      onCancelEdit={() => patchSearch({ edit: undefined, add: undefined })}
      addOpen={adding}
      onOpenAdd={() => patchSearch({ edit: true, add: true, drill: undefined })}
      onCloseAdd={() => patchSearch({ add: undefined })}
      drillKey={drillKey}
      onDrill={(key) => patchSearch({ drill: key ?? undefined })}
      exportOpen={search.export === true}
      onOpenExport={() => patchSearch({ export: true })}
      onCloseExport={() => patchSearch({ export: undefined })}
      drill={drill}
      onRetryDrill={() => void drillQuery.refetch()}
      widgetOptions={widgetOptions}
      onRetryOptions={() => void options.refetch()}
      onPreviewWidget={onPreviewWidget}
      sharing={sharingState}
      exportJob={exportJob}
      onCheckExport={() => void job.refetch()}
      onStartExport={onStartExport}
      onSaveLayout={onSaveLayout}
      onRemoveWidget={onRemoveWidget}
      onRename={onRename}
      onShare={onShare}
      onDelete={onDelete}
      onApplyLayout={onApplyLayout}
    />
  );
}
