/**
 * @notrelix/work-management-state — Work Management query keys.
 *
 * Every key is workspace-scoped via the canonical `workspaceQueryKey` helper.
 * No compatibility overload without workspaceId.
 */
import { workspaceQueryKey } from "@notrelix/query";

export const wmQueryKeys = {
  all: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "boards", "list"),
  workspaceList: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "boards", "workspace"),
  fullBoard: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "full",
      boardId,
    ),
  view: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "view",
      boardId,
    ),
  groups: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "groups",
      boardId,
    ),
  columns: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "columns",
      boardId,
    ),
  cardDetail: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "cards",
      "detail",
      cardId,
    ),
  cardUpdates: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "cards",
      "updates",
      cardId,
    ),
  cardFiles: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "cards", "files", cardId),
  cardComments: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "cards",
      "comments",
      cardId,
    ),
  cardActivity: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "cards",
      "activity",
      cardId,
    ),
  cardChecklists: (workspaceId: string, cardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "cards",
      "checklists",
      cardId,
    ),
} as const;
