import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { ReportLinks } from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { destinationHref } from "@/shell/workspace/workspace-destinations";
import {
  createReportsApi,
  type ReportsApiClient,
} from "./reports-api";

/** The report calls on the app's runtime client. */
export function useReportsApi() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(
    () => createReportsApi(runtimeClient.api as unknown as ReportsApiClient),
    [runtimeClient],
  );
}

/** Workspace-scoped query keys of the Reports pages. */
export const reportsKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "reports"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "reports", "list"),
  report: (workspaceId: string, reportId: string, assigneeId: string | null) =>
    workspaceQueryKey(workspaceId, "reports", "report", reportId, {
      assigneeId,
    }),
  reportAll: (workspaceId: string, reportId: string) =>
    workspaceQueryKey(workspaceId, "reports", "report", reportId),
  drill: (workspaceId: string, reportId: string, key: string) =>
    workspaceQueryKey(workspaceId, "reports", "drill", reportId, key),
  sharing: (workspaceId: string, reportId: string) =>
    workspaceQueryKey(workspaceId, "reports", "sharing", reportId),
  options: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "reports", "widget-options"),
  exportJob: (workspaceId: string, jobId: string) =>
    workspaceQueryKey(workspaceId, "reports", "export", jobId),
} as const;

/** Where the pages of a report and what it counts live. */
export function useReportLinks(): ReportLinks {
  const { workspaceId } = useWorkspaceContext();
  const navigate = useNavigate();
  return useMemo(() => {
    const base = `/workspaces/${encodeURIComponent(workspaceId)}/reports`;
    return {
      list: {
        href: base,
        onNavigate: () =>
          void navigate({
            to: "/workspaces/$workspaceId/reports",
            params: { workspaceId },
          } as never),
      },
      report: (reportId) => ({
        href: `${base}?report=${encodeURIComponent(reportId)}`,
        onNavigate: () =>
          void navigate({
            to: "/workspaces/$workspaceId/reports",
            params: { workspaceId },
            search: { report: reportId },
          } as never),
      }),
      board: (boardId) => {
        const destination = {
          section: "board",
          boardId,
          view: "table",
        } as const;
        return {
          href: destinationHref(workspaceId, destination),
          onNavigate: () =>
            void navigate({
              to: "/workspaces/$workspaceId/boards/$boardId",
              params: { workspaceId, boardId },
              search: { view: "table" },
            } as never),
        };
      },
      item: (boardId, itemId) => {
        const destination = {
          section: "board",
          boardId,
          item: itemId,
        } as const;
        return {
          href: destinationHref(workspaceId, destination),
          onNavigate: () =>
            void navigate({
              to: "/workspaces/$workspaceId/boards/$boardId",
              params: { workspaceId, boardId },
              search: { item: itemId },
            } as never),
        };
      },
    };
  }, [navigate, workspaceId]);
}
