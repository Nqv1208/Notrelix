import type { WorkspaceActivityItem } from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export interface ActivityEndpoints {
  workspaces: {
    activity: (workspaceId: string) => string;
  };
}

export interface WorkspaceActivityPage {
  readonly entries: readonly WorkspaceActivityItem[];
  readonly total: number;
}

/**
 * `Workspaces.Activity.GetWorkspaceActivity` returns
 * `{ data, total, page, pageSize }`. The entry shape has no contract yet (the
 * producer returns an empty untyped list, open item WX-HOME-001), so only
 * entries carrying every displayed field are kept.
 */
export function parseActivityPage(payload: unknown): WorkspaceActivityPage {
  const record =
    payload && typeof payload === "object"
      ? (payload as Record<string, unknown>)
      : {};
  const data = Array.isArray(record.data) ? record.data : [];
  const entries: WorkspaceActivityItem[] = [];
  for (const raw of data as Record<string, unknown>[]) {
    const id = raw?.id;
    const actor = raw?.actor ?? raw?.actorName;
    const action = raw?.action ?? raw?.summary;
    const target = raw?.target ?? raw?.targetName ?? "";
    const createdAt = raw?.createdAt ?? raw?.occurredAt;
    if (
      typeof id !== "string" ||
      typeof actor !== "string" ||
      typeof action !== "string" ||
      typeof target !== "string" ||
      typeof createdAt !== "string"
    )
      continue;
    entries.push({ id, actor, action, target, createdAt });
  }
  const total =
    typeof record.total === "number" ? record.total : entries.length;
  return { entries, total };
}

export function createActivityService(
  api: WorkspaceApiClient,
  endpoints: ActivityEndpoints,
) {
  return {
    async getPage(
      workspaceId: string,
      page = 1,
      pageSize = 20,
    ): Promise<WorkspaceActivityPage> {
      const query = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
      });
      return parseActivityPage(
        await api.get<unknown>(
          `${endpoints.workspaces.activity(workspaceId)}?${query}`,
        ),
      );
    },
  };
}
