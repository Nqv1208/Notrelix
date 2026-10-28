import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  DEFAULT_REPORT_WEEKS,
  WorkReportSurface,
  useFeatureAvailabilityCopy,
  type WorkReportState,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  createWorkReportApi,
  type WorkReportApiClient,
} from "./work-report-api";

function isForbidden(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    error.kind === "forbidden"
  );
}

/**
 * The read-only work report: across the boards the person can read, or one board. People who manage
 * spaces can narrow it to a department; the server decides, a refusal is shown as such.
 */
export function WorkReportPage() {
  const { workspaceId } = useWorkspaceContext();
  const { api: runtimeClient } = useAppRuntime();
  const copy = useFeatureAvailabilityCopy();
  const api = useMemo(
    () =>
      createWorkReportApi(runtimeClient.api as unknown as WorkReportApiClient),
    [runtimeClient],
  );
  const [boardId, setBoardId] = useState<string | null>(null);
  const [spaceId, setSpaceId] = useState<string | null>(null);
  const [weeks, setWeeks] = useState<number>(DEFAULT_REPORT_WEEKS);

  const boards = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "work-report", "boards"),
    queryFn: () => api.boards(workspaceId),
    retry: false,
  });
  const departments = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "work-report", "departments"),
    queryFn: () => api.departments(workspaceId),
    retry: false,
  });
  const report = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "work-report", "report", {
      boardId,
      spaceId,
      weeks,
    }),
    queryFn: () =>
      boardId
        ? api.board(boardId, weeks)
        : api.workspace(workspaceId, spaceId, weeks),
    retry: false,
  });

  const state: WorkReportState = report.data
    ? { status: "ready", data: report.data }
    : report.isError
      ? isForbidden(report.error)
        ? { status: "forbidden" }
        : { status: "error" }
      : { status: "loading" };

  return (
    <div className="p-7">
      <WorkReportSurface
        copy={copy}
        state={state}
        scope={{
          boards: boards.data ?? [],
          boardId,
          spaces: departments.data?.length ? departments.data : undefined,
          spaceId,
          weeks,
        }}
        onBoard={setBoardId}
        onSpace={setSpaceId}
        onWeeks={setWeeks}
        onRetry={() => void report.refetch()}
      />
    </div>
  );
}
