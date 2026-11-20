import type { WorkspaceApiClient } from "./workspace.service";

export interface ActivityEndpoints {
  workspaces: {
    activity: (workspaceId: string) => string;
  };
}

/** One line of the workspace feed (`WorkspaceActivityEntryDto`). `type` is the stable key; `title` the English fallback. */
export interface WorkspaceActivityEntry {
  readonly id: string;
  readonly type: string;
  readonly actorName: string | null;
  readonly actorAvatarUrl: string | null;
  readonly title: string;
  readonly subjectName: string | null;
  readonly occurredAt: string;
}

export interface WorkspaceActivityPage {
  readonly entries: readonly WorkspaceActivityEntry[];
  /** Where the next page starts; `null` when the feed has no more. */
  readonly nextCursor: string | null;
}

function text(value: unknown): string | null {
  return typeof value === "string" && value ? value : null;
}

/**
 * `Workspaces.Activity.GetWorkspaceActivity` returns `{ items, nextCursor }`: newest first, at most the
 * requested `limit` (default 20, at most 50). Only entries carrying every displayed field are kept.
 */
export function parseActivityPage(payload: unknown): WorkspaceActivityPage {
  const record =
    payload && typeof payload === "object"
      ? (payload as Record<string, unknown>)
      : {};
  const items = Array.isArray(record.items) ? record.items : [];
  const entries: WorkspaceActivityEntry[] = [];
  for (const raw of items as Record<string, unknown>[]) {
    const id = text(raw?.id);
    const type = text(raw?.type);
    const title = text(raw?.title);
    const occurredAt = text(raw?.occurredAt);
    if (!id || !type || !title || !occurredAt) continue;
    entries.push({
      id,
      type,
      title,
      occurredAt,
      actorName: text(raw.actorName),
      actorAvatarUrl: text(raw.actorAvatarUrl),
      subjectName: text(raw.subjectName),
    });
  }
  return { entries, nextCursor: text(record.nextCursor) };
}

export function createActivityService(
  api: WorkspaceApiClient,
  endpoints: ActivityEndpoints,
) {
  return {
    async getPage(
      workspaceId: string,
      cursor: string | null = null,
      limit = 20,
    ): Promise<WorkspaceActivityPage> {
      const query = new URLSearchParams({ limit: String(limit) });
      if (cursor) query.set("cursor", cursor);
      return parseActivityPage(
        await api.get<unknown>(
          `${endpoints.workspaces.activity(workspaceId)}?${query}`,
        ),
      );
    },
  };
}
