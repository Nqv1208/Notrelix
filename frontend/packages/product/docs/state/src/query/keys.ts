import { workspaceQueryKey } from "@notrelix/query";

export const docsQueryKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "documents"),
  tree: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "tree"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "list"),
  detail: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "detail", pageId),
  trash: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "trash"),
  breadcrumb: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "breadcrumb", pageId),
  blocks: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "blocks", pageId),
  comments: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "comments", pageId),
  history: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "history", pageId),
  permissions: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "permissions", pageId),
  backlinks: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "backlinks", pageId),
  versionSnapshot: (workspaceId: string, pageId: string, entryId: string) =>
    workspaceQueryKey(
      workspaceId,
      "documents",
      "version-snapshot",
      pageId,
      entryId,
    ),
  search: (workspaceId: string, query: string) =>
    workspaceQueryKey(workspaceId, "documents", "search", query),
  favorites: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "favorites"),
} as const;
