/**
 * @notrelix/work-management-state — Work Management query keys.
 *
 * Every key is workspace-scoped via the canonical `workspaceQueryKey` helper.
 * No compatibility overload without workspaceId.
 */
import { workspaceQueryKey } from "@notrelix/query";

export const wmQueryKeys = {
  boardChecklists: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "checklists",
      boardId,
    ),
  itemVotes: (workspaceId: string, itemId: string, fieldId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "item-votes",
      itemId,
      fieldId,
    ),
  itemViewers: (workspaceId: string, itemId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "item-viewers", itemId),
  all: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "boards", "list"),
  workspaceList: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "boards", "workspace"),
  boardLabels: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "labels",
      boardId,
    ),
  boardSavedFilters: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "saved-filters",
      boardId,
    ),
  boardForms: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "forms",
      boardId,
    ),
  form: (workspaceId: string, formId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "forms",
      "detail",
      formId,
    ),
  formSubmissions: (workspaceId: string, formId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "forms",
      "submissions",
      formId,
    ),
  boardTemplates: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "work-management", "templates"),
  boardTrash: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "trash",
      boardId,
    ),
  fullBoard: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "full",
      boardId,
    ),
  boardAccess: (workspaceId: string, boardId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "access",
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
  boardPreference: (workspaceId: string, boardId: string, viewId: string) =>
    workspaceQueryKey(
      workspaceId,
      "work-management",
      "boards",
      "preference",
      boardId,
      viewId,
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
