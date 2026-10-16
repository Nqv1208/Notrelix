import { useCallback, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ReportListSurface,
  useReportsCopy,
  type ReportListFilter,
  type SectionState,
  type ReportListItemDto,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import type { ReportsSearch } from "../../../../router/schemas/reports-search-schema";
import { reportsKeys, useReportLinks, useReportsApi } from "./use-reports";

const ROUTE = "/workspaces/$workspaceId/reports";

/** The list of reports (Final-Reports `danh-sach`). */
export function ReportListPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useReportsCopy();
  const formatters = useFormatters();
  const api = useReportsApi();
  const links = useReportLinks();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const search: ReportsSearch = useSearch({ from: ROUTE });
  const filter: ReportListFilter = search.tab ?? "all";
  const [createFailed, setCreateFailed] = useState(false);

  const list = useQuery({
    queryKey: reportsKeys.list(workspaceId),
    queryFn: () => api.list(workspaceId),
    retry: false,
  });
  const state = useMemo<SectionState<readonly ReportListItemDto[]>>(
    () =>
      list.data
        ? { status: "ready", data: list.data.reports }
        : list.isError
          ? { status: "error" }
          : { status: "loading" },
    [list.data, list.isError],
  );

  const create = useMutation({
    mutationFn: () => api.create(workspaceId),
    onSuccess: async (report) => {
      setCreateFailed(false);
      await queryClient.invalidateQueries({
        queryKey: reportsKeys.all(workspaceId),
      });
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: { report: report.id },
      } as never);
    },
    onError: () => setCreateFailed(true),
  });

  const onFilter = useCallback(
    (next: ReportListFilter) =>
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: { tab: next === "all" ? undefined : next },
        replace: true,
      } as never),
    [navigate, workspaceId],
  );

  return (
    <ReportListSurface
      copy={copy}
      formatters={formatters}
      links={links}
      workspaceName={workspace?.name ?? null}
      state={state}
      now={list.data?.serverTime ?? new Date(0).toISOString()}
      timeZone="Asia/Ho_Chi_Minh"
      filter={filter}
      onFilter={onFilter}
      creating={create.isPending}
      createFailed={createFailed}
      onCreate={() => create.mutate()}
      onRetry={() => void list.refetch()}
    />
  );
}
