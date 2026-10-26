import type { OperationResponse } from "@notrelix/contracts";
import type { WorkReportDto } from "@notrelix/features-workspace/web";

/** The calls behind the work report page; every call is quiet, the page says what went wrong. */
export interface WorkReportApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;
const root = (workspaceId: string) =>
  `/workspaces/${encodeURIComponent(workspaceId)}`;

type BoardReport = NonNullable<
  OperationResponse<"Analytics.Reports.GetBoardReport">
>;
type WorkspaceReport = NonNullable<
  OperationResponse<"Analytics.Reports.GetWorkspaceReport">
>;
type Report =
  NonNullable<BoardReport["report"]> | NonNullable<WorkspaceReport["report"]>;

function toReport(row: Report | undefined): WorkReportDto {
  return {
    total: row?.total ?? 0,
    open: row?.open ?? 0,
    done: row?.done ?? 0,
    overdue: row?.overdue ?? 0,
    byStatus: (row?.byStatus ?? []).map((s) => ({
      optionId: s.optionId ?? null,
      name: s.name ?? null,
      color: s.color ?? null,
      category: s.category ?? "None",
      count: s.count ?? 0,
    })),
    byAssignee: (row?.byAssignee ?? []).map((a) => ({
      userId: a.userId ?? null,
      name: a.name ?? null,
      items: a.items ?? 0,
      open: a.open ?? 0,
      overdue: a.overdue ?? 0,
    })),
    completedPerWeek: (row?.completedPerWeek ?? []).map((w) => ({
      weekStart: w.weekStart ?? "",
      completed: w.completed ?? 0,
    })),
    averageCompletionSeconds: row?.averageCompletionSeconds ?? null,
  };
}

export interface ReportBoardOption {
  readonly id: string;
  readonly title: string;
}

export interface ReportSpaceOption {
  readonly id: string;
  readonly name: string;
}

type BoardRow = { id?: string; title?: string | null; isArchived?: boolean };
type SpaceRow = {
  id?: string;
  name?: string | null;
  spaceType?: string | null;
  isArchived?: boolean;
};

export function createWorkReportApi(api: WorkReportApiClient) {
  return {
    /** The work report of one board the caller can read. */
    async board(boardId: string, weeks: number): Promise<WorkReportDto> {
      const row = await api.get<BoardReport>(
        `/boards/${encodeURIComponent(boardId)}/report?weeks=${weeks}`,
        QUIET,
      );
      return toReport(row.report);
    },
    /** The work report across the readable boards, optionally narrowed to a department. */
    async workspace(
      workspaceId: string,
      spaceId: string | null,
      weeks: number,
    ): Promise<WorkReportDto> {
      const query = `?weeks=${weeks}${
        spaceId ? `&spaceId=${encodeURIComponent(spaceId)}` : ""
      }`;
      const row = await api.get<WorkspaceReport>(
        `${root(workspaceId)}/work-report${query}`,
        QUIET,
      );
      return toReport(row.report);
    },
    /** The boards the caller can read, for the board picker. */
    async boards(workspaceId: string): Promise<ReportBoardOption[]> {
      const rows =
        (await api.get<BoardRow[] | null>(
          `${root(workspaceId)}/boards`,
          QUIET,
        )) ?? [];
      return rows
        .filter((b) => b.id && !b.isArchived)
        .map((b) => ({ id: b.id ?? "", title: b.title ?? "" }));
    },
    /** The department spaces, for the admin filter. */
    async departments(workspaceId: string): Promise<ReportSpaceOption[]> {
      const rows =
        (await api.get<SpaceRow[] | null>(
          `${root(workspaceId)}/spaces/tree`,
          QUIET,
        )) ?? [];
      return rows
        .filter((s) => s.id && !s.isArchived && s.spaceType === "Department")
        .map((s) => ({ id: s.id ?? "", name: s.name ?? "" }));
    },
  };
}
