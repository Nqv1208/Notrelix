import { workspaceQueryKey } from "@notrelix/query";

export const docsQueryKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "documents"),
  tree: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "tree"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "list"),
  detail: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "detail", pageId),
  breadcrumb: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "breadcrumb", pageId),
  blocks: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "blocks", pageId),
  comments: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "comments", pageId),
  history: (workspaceId: string, pageId: string) =>
    workspaceQueryKey(workspaceId, "documents", "history", pageId),
  search: (workspaceId: string, query: string) =>
    workspaceQueryKey(workspaceId, "documents", "search", query),
  favorites: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "documents", "favorites"),
} as const;
